using System.IO.Compression;
using Area.Backup.Core.Enums;
using Area.Backup.Core.Interfaces;
using Area.Backup.Core.Models;
using CoreCompLevel = Area.Backup.Core.Enums.CompressionLevel;
using SysZipLevel = System.IO.Compression.CompressionLevel;

namespace Area.Backup.Infrastructure.Compression;

/// <summary>
/// High-performance stream-based ZIP archive provider.
/// Writes file streams directly to the zip package without intermediate disk copies.
/// </summary>
public sealed class ZipCompressionProvider : ICompressionProvider
{
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.Zip;

    public async Task CreateArchiveAsync(
        string destinationArchivePath,
        IEnumerable<ArchiveEntrySource> entries,
        CompressionOptions options,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationArchivePath);
        ArgumentNullException.ThrowIfNull(entries);

        var destinationDir = Path.GetDirectoryName(destinationArchivePath);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        var zipLevel = options.Level switch
        {
            CoreCompLevel.Fastest => SysZipLevel.Fastest,
            CoreCompLevel.NoCompression => SysZipLevel.NoCompression,
            CoreCompLevel.SmallestSize => SysZipLevel.SmallestSize,
            _ => SysZipLevel.Optimal
        };

        // Open archive stream with sequential scan and buffer
        using var fileStream = new FileStream(
            destinationArchivePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 65536,
            useAsync: true);

        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: false);

        var buffer = new byte[65536];

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedEntryPath = entry.EntryPathInArchive.Replace('\\', '/').TrimStart('/');
            var zipEntry = archive.CreateEntry(normalizedEntryPath, zipLevel);
            zipEntry.LastWriteTime = entry.LastWriteTimeUtc;

            using var entryStream = zipEntry.Open();

            if (entry.DirectContentBytes != null)
            {
                await entryStream.WriteAsync(entry.DirectContentBytes, cancellationToken);
            }
            else if (entry.DirectStream != null)
            {
                int read;
                while ((read = await entry.DirectStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await entryStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
            else if (!string.IsNullOrEmpty(entry.SourceFilePath) && File.Exists(entry.SourceFilePath))
            {
                using var sourceStream = new FileStream(
                    entry.SourceFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 65536,
                    useAsync: true);

                int read;
                while ((read = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await entryStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
        }
    }

    public async Task ExtractEntryAsync(
        string archivePath,
        string entryName,
        string destinationFilePath,
        bool overwrite = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFilePath);

        var normalizedTarget = entryName.Replace('\\', '/').TrimStart('/');
        var destinationDir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        using var archive = ZipFile.OpenRead(archivePath);
        var entry = archive.Entries.FirstOrDefault(e => string.Equals(e.FullName.Replace('\\', '/').TrimStart('/'), normalizedTarget, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
            throw new FileNotFoundException($"Entry '{entryName}' not found in backup archive '{archivePath}'.");

        if (File.Exists(destinationFilePath) && !overwrite)
            return;

        using var entryStream = entry.Open();
        using var outStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);

        await entryStream.CopyToAsync(outStream, cancellationToken);
        File.SetLastWriteTimeUtc(destinationFilePath, entry.LastWriteTime.UtcDateTime);
    }

    public async Task ExtractAllAsync(
        string archivePath,
        string destinationDirectory,
        Func<string, bool>? filter = null,
        bool overwrite = true,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        using var archive = ZipFile.OpenRead(archivePath);
        var entries = archive.Entries.Where(e => !e.FullName.EndsWith('/') && !e.FullName.EndsWith('\\')).ToList();

        if (filter != null)
        {
            entries = entries.Where(e => filter(e.FullName)).ToList();
        }

        var buffer = new byte[65536];
        long totalBytes = entries.Sum(e => e.Length);
        long processedBytes = 0;
        long processedFiles = 0;
        var start = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var targetPath = Path.Combine(destinationDirectory, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            if (File.Exists(targetPath) && !overwrite)
            {
                processedBytes += entry.Length;
                processedFiles++;
                continue;
            }

            using (var entryStream = entry.Open())
            using (var outStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true))
            {
                int read;
                while ((read = await entryStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await outStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    processedBytes += read;

                    progress?.Report(BackupProgress.Create(
                        BackupStage.Writing,
                        processedFiles,
                        entries.Count,
                        processedBytes,
                        totalBytes,
                        entry.FullName,
                        DateTime.UtcNow - start));
                }
            }

            File.SetLastWriteTimeUtc(targetPath, entry.LastWriteTime.UtcDateTime);
            processedFiles++;
        }
    }

    public IReadOnlyList<string> ListEntries(string archivePath)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        return archive.Entries.Select(e => e.FullName).ToList();
    }

    public Stream? OpenEntryReadStream(string archivePath, string entryName)
    {
        var normalizedTarget = entryName.Replace('\\', '/').TrimStart('/');
        var archive = ZipFile.OpenRead(archivePath);
        var entry = archive.Entries.FirstOrDefault(e => string.Equals(e.FullName.Replace('\\', '/').TrimStart('/'), normalizedTarget, StringComparison.OrdinalIgnoreCase));
        return entry?.Open();
    }
}
