using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Area.Backup.Core.Exceptions;
using Area.Backup.Core.Interfaces;
using Area.Backup.Core.Models.Manifest;

namespace Area.Backup.Infrastructure.Manifest;

/// <summary>
/// Service responsible for serializing, parsing, and reading versioned backup manifests from archives.
/// </summary>
public sealed class JsonManifestService : IManifestService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Serialize(BackupManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return JsonSerializer.Serialize(manifest, JsonOptions);
    }

    public BackupManifest Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        try
        {
            var manifest = JsonSerializer.Deserialize<BackupManifest>(json, JsonOptions);
            if (manifest == null)
                throw new BackupIntegrityException("Failed to deserialize backup manifest: manifest content was null.");

            if (manifest.FormatVersion > 1)
                throw new BackupIntegrityException($"Unsupported backup format version: {manifest.FormatVersion}. Maximum supported format version is 1.");

            return manifest;
        }
        catch (JsonException ex)
        {
            throw new BackupIntegrityException($"Backup manifest JSON is corrupted or invalid: {ex.Message}", ex);
        }
    }

    public async Task<BackupManifest> ReadManifestFromArchiveAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);

        if (!File.Exists(archivePath))
            throw new FileNotFoundException($"Backup archive file was not found: {archivePath}");

        using var zip = ZipFile.OpenRead(archivePath);
        var entry = zip.GetEntry("manifest.json");
        if (entry == null)
            throw new BackupIntegrityException($"Archive is missing required 'manifest.json' entry.", archivePath);

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(cancellationToken);

        return Deserialize(json);
    }
}
