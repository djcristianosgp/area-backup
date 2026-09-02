using System.Runtime.CompilerServices;
using Area.Backup.Core.Interfaces;
using Area.Backup.Core.Models;

namespace Area.Backup.Infrastructure.Scanning;

/// <summary>
/// Scans local and network file systems without buffering whole file contents.
/// </summary>
public sealed class FileSystemScanner : IFileScanner
{
    public async IAsyncEnumerable<ScannedFile> ScanSourcesAsync(
        IReadOnlyList<BackupSource> sources,
        IReadOnlyList<BackupExclusion> exclusions,
        PerformanceOptions performance,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var matcher = new ExclusionMatcher(exclusions);

        foreach (var source in sources)
        {
            if (!source.Enabled) continue;
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(source.Path))
            {
                if (performance.FailOnLockedFile)
                    throw new DirectoryNotFoundException($"Source directory '{source.Path}' was not found.");
                continue;
            }

            var visitedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await foreach (var file in ScanDirectoryRecursiveAsync(source.Id, source.Path, source.Path, matcher, visitedDirectories, cancellationToken))
            {
                yield return file;
            }
        }
    }

    private async IAsyncEnumerable<ScannedFile> ScanDirectoryRecursiveAsync(
        string sourceId,
        string rootSourcePath,
        string currentDirectory,
        ExclusionMatcher matcher,
        HashSet<string> visitedDirectories,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCurrent = Path.GetFullPath(currentDirectory);
        if (!visitedDirectories.Add(normalizedCurrent))
        {
            // Circular link / junction loop detected - avoid infinite loop
            yield break;
        }

        DirectoryInfo dirInfo;
        try
        {
            dirInfo = new DirectoryInfo(currentDirectory);
            if (!dirInfo.Exists) yield break;

            // Avoid following reparse points (symlinks/junctions) if they might loop
            if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0 && !string.Equals(rootSourcePath, currentDirectory, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }
        }
        catch
        {
            yield break;
        }

        // 1. Enumerate files in current directory
        List<FileInfo> fileList;
        try
        {
            fileList = dirInfo.EnumerateFiles().ToList();
        }
        catch
        {
            fileList = new List<FileInfo>();
        }

        foreach (var file in fileList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ScannedFile? scanned = null;
            try
            {
                if (!matcher.IsFileExcluded(file.FullName, file.Name))
                {
                    var relativePath = Path.GetRelativePath(rootSourcePath, file.FullName)
                                           .Replace('/', Path.DirectorySeparatorChar);

                    scanned = new ScannedFile(
                        SourceId: sourceId,
                        FullPath: file.FullName,
                        RelativePath: relativePath,
                        Size: file.Length,
                        LastWriteTimeUtc: file.LastWriteTimeUtc);
                }
            }
            catch
            {
                // Inaccessible file metadata
            }

            if (scanned != null)
            {
                yield return scanned;
            }
        }

        // 2. Enumerate subdirectories
        List<DirectoryInfo> subDirList;
        try
        {
            subDirList = dirInfo.EnumerateDirectories().ToList();
        }
        catch
        {
            subDirList = new List<DirectoryInfo>();
        }

        foreach (var subDir in subDirList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool isExcluded = false;
            try
            {
                isExcluded = matcher.IsDirectoryExcluded(subDir.FullName, subDir.Name);
            }
            catch
            {
                isExcluded = true;
            }

            if (!isExcluded)
            {
                await foreach (var nested in ScanDirectoryRecursiveAsync(sourceId, rootSourcePath, subDir.FullName, matcher, visitedDirectories, cancellationToken))
                {
                    yield return nested;
                }
            }
        }
    }
}
