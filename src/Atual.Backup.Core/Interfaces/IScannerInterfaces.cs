using Atual.Backup.Core.Models;

namespace Atual.Backup.Core.Interfaces;

/// <summary>
/// Scanned file item metadata.
/// </summary>
public sealed record ScannedFile(
    string SourceId,
    string FullPath,
    string RelativePath,
    long Size,
    DateTime LastWriteTimeUtc);

/// <summary>
/// Scans source directories and network shares, applying exclusions and loop protections.
/// </summary>
public interface IFileScanner
{
    IAsyncEnumerable<ScannedFile> ScanSourcesAsync(
        IReadOnlyList<BackupSource> sources,
        IReadOnlyList<BackupExclusion> exclusions,
        PerformanceOptions performance,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Source provider for file change detection (e.g. USN Journal or Full Scanner).
/// </summary>
public interface IChangeSource
{
    string Name { get; }
    bool IsAvailable(string path);
    IAsyncEnumerable<ScannedFile> GetChangedCandidatesAsync(
        BackupSource source,
        DateTime? sinceUtc,
        IReadOnlyList<BackupExclusion> exclusions,
        CancellationToken cancellationToken = default);
}
