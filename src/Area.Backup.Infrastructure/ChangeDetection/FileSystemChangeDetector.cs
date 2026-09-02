using Area.Backup.Core.Enums;
using Area.Backup.Core.Interfaces;
using Area.Backup.Core.Models;
using Area.Backup.Core.Models.Manifest;

namespace Area.Backup.Infrastructure.ChangeDetection;

/// <summary>
/// High performance tiered change detection engine.
/// Compares filesystem state against prior manifests to pinpoint Added, Modified, and Deleted files.
/// </summary>
public sealed class FileSystemChangeDetector : IChangeDetector
{
    private readonly IChecksumService _checksumService;

    public FileSystemChangeDetector(IChecksumService? checksumService = null)
    {
        _checksumService = checksumService ?? new Hashing.Sha256ChecksumService();
    }

    public async Task<ChangeDetectionResult> DetectChangesAsync(
        IReadOnlyList<ScannedFile> currentFiles,
        IReadOnlyList<BackupSource> sources,
        BackupManifest? previousManifest,
        IncrementalOptions options,
        CancellationToken cancellationToken = default)
    {
        var changedFiles = new List<FileChangeCandidate>();
        var deletedFiles = new List<ManifestDeletedFile>();

        long totalFilesScanned = currentFiles.Count;
        long totalBytesScanned = currentFiles.Sum(f => f.Size);

        // Case 1: No previous manifest (Initial or standalone Full) -> All files are Added
        if (previousManifest == null || previousManifest.Files.Count == 0)
        {
            foreach (var file in currentFiles)
            {
                changedFiles.Add(new FileChangeCandidate(file, FileChangeType.Added));
            }

            return new ChangeDetectionResult
            {
                ChangedFiles = changedFiles,
                DeletedFiles = deletedFiles,
                TotalFilesScanned = totalFilesScanned,
                TotalBytesScanned = totalBytesScanned
            };
        }

        // Case 2: Incremental comparison against previous state
        var prevFileMap = new Dictionary<string, ManifestFileEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in previousManifest.Files)
        {
            var key = MakeFileKey(file.SourceId, file.RelativePath);
            prevFileMap[key] = file;
        }

        var currentFileKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Detect Added and Modified files
        foreach (var file in currentFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = MakeFileKey(file.SourceId, file.RelativePath);
            currentFileKeys.Add(key);

            if (!prevFileMap.TryGetValue(key, out var prevEntry))
            {
                // New file
                changedFiles.Add(new FileChangeCandidate(file, FileChangeType.Added));
            }
            else
            {
                // File existed previously -> Check size and timestamp
                bool sizeChanged = file.Size != prevEntry.Size;
                // Allow up to 2 seconds tolerance for filesystem timestamp precision differences (FAT32/Zip)
                bool timeChanged = Math.Abs((file.LastWriteTimeUtc - prevEntry.LastWriteTimeUtc).TotalSeconds) > 2.0;

                if (sizeChanged || timeChanged)
                {
                    changedFiles.Add(new FileChangeCandidate(file, FileChangeType.Modified));
                }
                else
                {
                    // Unchanged: preserve known hash without reading payload
                }
            }
        }

        // Detect Deleted files
        var activeSourceIds = sources.Where(s => s.Enabled).Select(s => s.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, prevEntry) in prevFileMap)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Only check deletions for sources that were actively scanned
            if (activeSourceIds.Contains(prevEntry.SourceId) && !currentFileKeys.Contains(key))
            {
                deletedFiles.Add(new ManifestDeletedFile
                {
                    SourceId = prevEntry.SourceId,
                    RelativePath = prevEntry.RelativePath,
                    DeletedAtUtc = DateTime.UtcNow
                });
            }
        }

        return new ChangeDetectionResult
        {
            ChangedFiles = changedFiles,
            DeletedFiles = deletedFiles,
            TotalFilesScanned = totalFilesScanned,
            TotalBytesScanned = totalBytesScanned
        };
    }

    private static string MakeFileKey(string sourceId, string relativePath) =>
        $"{sourceId}::{relativePath.Replace('/', '\\').Trim('\\')}";
}
