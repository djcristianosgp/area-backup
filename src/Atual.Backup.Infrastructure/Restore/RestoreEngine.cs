using System.Diagnostics;
using System.IO.Compression;
using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Exceptions;
using Atual.Backup.Core.Interfaces;
using Atual.Backup.Core.Models;
using Atual.Backup.Core.Models.Manifest;

namespace Atual.Backup.Infrastructure.Restore;

/// <summary>
/// High-performance restore engine capable of reconstructing point-in-time filesystem state
/// across Full and Incremental backup chains.
/// </summary>
public sealed class RestoreEngine : IRestoreEngine
{
    private readonly IManifestService _manifestService;
    private readonly IChecksumService _checksumService;

    public RestoreEngine(IManifestService? manifestService = null, IChecksumService? checksumService = null)
    {
        _manifestService = manifestService ?? new Manifest.JsonManifestService();
        _checksumService = checksumService ?? new Hashing.Sha256ChecksumService();
    }

    public async Task<RestoreResult> RestoreAsync(
        string targetBackupPath,
        RestoreOptions options,
        ICatalogRepository catalog,
        IBackupStorage storage,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetBackupPath);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(storage);

        if (!File.Exists(targetBackupPath))
            throw new FileNotFoundException($"Target backup archive was not found: {targetBackupPath}");

        var stopwatch = Stopwatch.StartNew();
        var targetManifest = await _manifestService.ReadManifestFromArchiveAsync(targetBackupPath, cancellationToken);

        // 1. Resolve full dependency chain (Full -> Inc 1 -> Inc 2 -> ... -> Target)
        var backupChainFiles = new List<string>();

        if (targetManifest.Type == BackupType.Full)
        {
            backupChainFiles.Add(targetBackupPath);
        }
        else
        {
            var chainEntries = await catalog.GetBackupChainAsync(targetManifest.BackupId, cancellationToken);
            if (chainEntries.Count > 0)
            {
                foreach (var entry in chainEntries)
                {
                    var path = Path.Combine(storage.RootPath, entry.RelativeFilePath);
                    if (!File.Exists(path))
                    {
                        // Fallback check if it's the target path itself
                        if (string.Equals(entry.BackupId, targetManifest.BackupId, StringComparison.OrdinalIgnoreCase))
                            path = targetBackupPath;
                        else
                            throw new BackupRestoreException($"Required backup file in chain was not found: {path} (Backup ID: {entry.BackupId})");
                    }
                    backupChainFiles.Add(path);
                }
            }
            else
            {
                // Standalone incremental without catalog entry: attempt restoring target directly
                backupChainFiles.Add(targetBackupPath);
            }
        }

        var warnings = new List<BackupWarning>();
        var errors = new List<BackupError>();
        long totalFilesRestored = 0;
        long totalFilesOverwritten = 0;
        long totalFilesDeleted = 0;
        long totalBytesRestored = 0;

        var restoredFilesManifest = new Dictionary<string, ManifestFileEntry>(StringComparer.OrdinalIgnoreCase);

        // 2. Sequential overlay of chain
        for (int i = 0; i < backupChainFiles.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var archivePath = backupChainFiles[i];
            var currentManifest = await _manifestService.ReadManifestFromArchiveAsync(archivePath, cancellationToken);

            var sourceMap = currentManifest.Sources.ToDictionary(s => s.SourceId, s => s.OriginalPath, StringComparer.OrdinalIgnoreCase);

            using var zip = ZipFile.OpenRead(archivePath);

            // Extract payload files
            foreach (var fileEntry in currentManifest.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Apply source filter
                if (!string.IsNullOrEmpty(options.SourceId) && !string.Equals(fileEntry.SourceId, options.SourceId, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Apply relative path filter
                if (!string.IsNullOrEmpty(options.RelativePathFilter) && !fileEntry.RelativePath.StartsWith(options.RelativePathFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                var destinationFilePath = ResolveDestinationPath(options, fileEntry, sourceMap);
                var destDir = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                var zipEntry = zip.GetEntry(fileEntry.ArchiveEntryPath.Replace('\\', '/').TrimStart('/'));
                if (zipEntry == null)
                {
                    errors.Add(new BackupError("ENTRY_MISSING", $"Entry '{fileEntry.ArchiveEntryPath}' not found in archive '{archivePath}'.", destinationFilePath));
                    continue;
                }

                bool exists = File.Exists(destinationFilePath);
                if (exists && !options.OverwriteExisting)
                    continue;

                if (exists) totalFilesOverwritten++;

                using (var src = zipEntry.Open())
                using (var dst = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true))
                {
                    await src.CopyToAsync(dst, cancellationToken);
                }

                File.SetLastWriteTimeUtc(destinationFilePath, fileEntry.LastWriteTimeUtc);
                totalFilesRestored++;
                totalBytesRestored += fileEntry.Size;

                restoredFilesManifest[destinationFilePath] = fileEntry;

                progress?.Report(BackupProgress.Create(
                    BackupStage.Writing,
                    totalFilesRestored,
                    totalFilesRestored,
                    totalBytesRestored,
                    totalBytesRestored,
                    fileEntry.RelativePath,
                    stopwatch.Elapsed));
            }

            // Apply deletions in incremental points
            if (options.ApplyDeletions && currentManifest.DeletedFiles.Count > 0)
            {
                foreach (var del in currentManifest.DeletedFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!string.IsNullOrEmpty(options.SourceId) && !string.Equals(del.SourceId, options.SourceId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var dummyEntry = new ManifestFileEntry { SourceId = del.SourceId, RelativePath = del.RelativePath };
                    var targetFileToDelete = ResolveDestinationPath(options, dummyEntry, sourceMap);

                    if (File.Exists(targetFileToDelete))
                    {
                        try
                        {
                            File.Delete(targetFileToDelete);
                            totalFilesDeleted++;
                            restoredFilesManifest.Remove(targetFileToDelete);
                        }
                        catch (Exception ex)
                        {
                            warnings.Add(new BackupWarning("DELETE_FAILED", $"Could not delete file during incremental restore: {ex.Message}", targetFileToDelete));
                        }
                    }
                }
            }
        }

        // 3. Optional post-restore checksum validation
        if (options.VerifyChecksumsAfterRestore)
        {
            foreach (var (filePath, expectedEntry) in restoredFilesManifest)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(filePath)) continue;

                var hash = await _checksumService.ComputeSha256Async(filePath, cancellationToken: cancellationToken);
                if (!string.Equals(hash, expectedEntry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new BackupError("CHECKSUM_MISMATCH", $"Restored file SHA-256 mismatch for '{filePath}'. Expected: {expectedEntry.Sha256}, Got: {hash}", filePath));
                }
            }
        }

        stopwatch.Stop();

        return new RestoreResult
        {
            Success = errors.Count == 0,
            TargetBackupId = targetManifest.BackupId,
            DestinationPath = options.DestinationPath,
            BackupsInChainCount = backupChainFiles.Count,
            FilesRestored = totalFilesRestored,
            FilesOverwritten = totalFilesOverwritten,
            FilesDeleted = totalFilesDeleted,
            BytesRestored = totalBytesRestored,
            Duration = stopwatch.Elapsed,
            Warnings = warnings,
            Errors = errors
        };
    }

    private static string ResolveDestinationPath(
        RestoreOptions options,
        ManifestFileEntry entry,
        IReadOnlyDictionary<string, string> sourceMap)
    {
        if (options.RestoreToOriginalLocations && sourceMap.TryGetValue(entry.SourceId, out var originalSourcePath))
        {
            return Path.Combine(originalSourcePath, entry.RelativePath);
        }

        var baseDir = string.IsNullOrWhiteSpace(options.DestinationPath) ? Directory.GetCurrentDirectory() : options.DestinationPath;
        return Path.Combine(baseDir, entry.SourceId, entry.RelativePath);
    }
}
