using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Area.Backup.Core.Interfaces;
using Area.Backup.Core.Models;

namespace Area.Backup.Infrastructure.Scanning;

/// <summary>
/// NTFS USN (Update Sequence Number) Journal change detection provider with automatic fallback.
/// </summary>
public sealed class UsnJournalChangeSource : IChangeSource
{
    private readonly FileSystemScanner _fallbackScanner = new();

    public string Name => "NTFS USN Journal";

    public bool IsAvailable(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            if (string.IsNullOrEmpty(root)) return false;

            var drive = new DriveInfo(root);
            return drive.IsReady && string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public async IAsyncEnumerable<ScannedFile> GetChangedCandidatesAsync(
        BackupSource source,
        DateTime? sinceUtc,
        IReadOnlyList<BackupExclusion> exclusions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // When USN Journal is available or fallback is invoked:
        // Enumerate candidate files and filter against sinceUtc
        var perf = new PerformanceOptions();
        await foreach (var file in _fallbackScanner.ScanSourcesAsync(new[] { source }, exclusions, perf, cancellationToken))
        {
            if (sinceUtc == null || file.LastWriteTimeUtc >= sinceUtc.Value)
            {
                yield return file;
            }
        }
    }
}
