using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Models;
using Atual.Backup.Core.Models.Manifest;

namespace Atual.Backup.Core.Interfaces;

/// <summary>
/// Stream-level archive writer abstraction (ZIP, Zstd, etc.).
/// </summary>
public interface ICompressionProvider
{
    CompressionAlgorithm Algorithm { get; }

    Task CreateArchiveAsync(
        string destinationArchivePath,
        IEnumerable<ArchiveEntrySource> entries,
        CompressionOptions options,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task ExtractEntryAsync(
        string archivePath,
        string entryName,
        string destinationFilePath,
        bool overwrite = true,
        CancellationToken cancellationToken = default);

    Task ExtractAllAsync(
        string archivePath,
        string destinationDirectory,
        Func<string, bool>? filter = null,
        bool overwrite = true,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);

    IReadOnlyList<string> ListEntries(string archivePath);
    Stream? OpenEntryReadStream(string archivePath, string entryName);
}

/// <summary>
/// Represents a file or metadata entry to be written into the package.
/// </summary>
public sealed class ArchiveEntrySource
{
    public string EntryPathInArchive { get; init; } = string.Empty;
    public string? SourceFilePath { get; init; }
    public byte[]? DirectContentBytes { get; init; }
    public Stream? DirectStream { get; init; }
    public DateTime LastWriteTimeUtc { get; init; } = DateTime.UtcNow;
    public long Size { get; init; }
}

/// <summary>
/// Serializes and deserializes versioned JSON manifests.
/// </summary>
public interface IManifestService
{
    string Serialize(BackupManifest manifest);
    BackupManifest Deserialize(string json);
    Task<BackupManifest> ReadManifestFromArchiveAsync(string archivePath, CancellationToken cancellationToken = default);
}
