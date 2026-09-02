using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;
using Area.Backup.Core.Models.Manifest;

namespace Area.Backup.Core.Interfaces;

/// <summary>
/// Classified file candidate evaluated for backup inclusion.
/// </summary>
public sealed record FileChangeCandidate(
    ScannedFile ScannedFile,
    FileChangeType ChangeType,
    string? KnownHashSha256 = null);

/// <summary>
/// Evaluates scanned files against prior backup manifests / catalog to detect additions, modifications, and deletions.
/// </summary>
public interface IChangeDetector
{
    Task<ChangeDetectionResult> DetectChangesAsync(
        IReadOnlyList<ScannedFile> currentFiles,
        IReadOnlyList<BackupSource> sources,
        BackupManifest? previousManifest,
        IncrementalOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result container for detected file changes.
/// </summary>
public sealed class ChangeDetectionResult
{
    public List<FileChangeCandidate> ChangedFiles { get; init; } = new();
    public List<ManifestDeletedFile> DeletedFiles { get; init; } = new();
    public long TotalFilesScanned { get; init; }
    public long TotalBytesScanned { get; init; }
    public long TotalBytesToBackup => ChangedFiles.Sum(f => f.ScannedFile.Size);
    public long FilesAdded => ChangedFiles.Count(f => f.ChangeType == FileChangeType.Added);
    public long FilesModified => ChangedFiles.Count(f => f.ChangeType == FileChangeType.Modified);
    public long FilesDeleted => DeletedFiles.Count;
    public long FilesUnchanged => TotalFilesScanned - ChangedFiles.Count;
}

/// <summary>
/// Computes cryptographic checksums.
/// </summary>
public interface IChecksumService
{
    Task<string> ComputeSha256Async(string filePath, int bufferSize = 65536, CancellationToken cancellationToken = default);
    Task<string> ComputeSha256Async(Stream stream, int bufferSize = 65536, CancellationToken cancellationToken = default);
}
