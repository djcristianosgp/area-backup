using Area.Backup.Core.Enums;
using Area.Backup.Core.Exceptions;
using Area.Backup.Core.Interfaces;

namespace Area.Backup.Infrastructure.Storage;

/// <summary>
/// Manages the local or network filesystem storage directory layout and atomic file operations.
/// Layout:
///   [RepositoryRoot]/
///     catalog.db
///     logs/
///     YYYY/
///       MM/
///         YYYYMMDD-HHmmss-full.backup
///         YYYYMMDD-HHmmss-incremental.backup
/// </summary>
public sealed class LocalFileSystemStorage : IBackupStorage
{
    public string RootPath { get; }

    public LocalFileSystemStorage(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = Path.GetFullPath(rootPath.TrimEnd('\\', '/'));

        if (!Directory.Exists(RootPath))
        {
            Directory.CreateDirectory(RootPath);
        }
    }

    public string GetCatalogPath() => Path.Combine(RootPath, "catalog.db");

    public string GenerateBackupFilePath(string backupId, BackupType type, DateTime dateUtc)
    {
        var year = dateUtc.ToString("yyyy");
        var month = dateUtc.ToString("MM");
        var typeSuffix = type == BackupType.Full ? "full" : "incremental";
        var fileName = $"{backupId}-{typeSuffix}.backup";

        var folder = Path.Combine(RootPath, year, month);
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        return Path.Combine(folder, fileName);
    }

    public string GetTempFilePath(string finalBackupFilePath) => $"{finalBackupFilePath}.tmp";

    public void CommitTempFile(string tempFilePath, string finalFilePath)
    {
        if (!File.Exists(tempFilePath))
            throw new BackupStorageException($"Temporary backup file does not exist: {tempFilePath}");

        if (File.Exists(finalFilePath))
        {
            File.Delete(finalFilePath);
        }

        File.Move(tempFilePath, finalFilePath);
    }

    public void CleanupTempFiles()
    {
        try
        {
            if (!Directory.Exists(RootPath)) return;

            var tempFiles = Directory.EnumerateFiles(RootPath, "*.tmp", SearchOption.AllDirectories);
            foreach (var temp in tempFiles)
            {
                try
                {
                    File.Delete(temp);
                }
                catch
                {
                    // Ignore locked files being processed
                }
            }
        }
        catch
        {
            // Ignore scan failures during cleanup
        }
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public bool FileExists(string filePath) => File.Exists(filePath);

    public long GetAvailableFreeSpaceBytes()
    {
        try
        {
            var root = Path.GetPathRoot(RootPath);
            if (string.IsNullOrEmpty(root)) return long.MaxValue;

            var drive = new DriveInfo(root);
            return drive.AvailableFreeSpace;
        }
        catch
        {
            return long.MaxValue;
        }
    }
}
