using Area.Backup.Core.Models;
using Area.Backup.Core.Models.Manifest;

namespace Area.Backup.Core.Interfaces;

/// <summary>
/// Persistent database repository managing the history of all backup sets and files.
/// </summary>
public interface ICatalogRepository : IDisposable, IAsyncDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task RegisterBackupAsync(
        BackupCatalogEntry entry,
        BackupManifest manifest,
        CancellationToken cancellationToken = default);

    Task<BackupCatalogEntry?> GetBackupByIdAsync(string backupId, CancellationToken cancellationToken = default);

    Task<BackupCatalogEntry?> GetLatestBackupAsync(CancellationToken cancellationToken = default);

    Task<BackupCatalogEntry?> GetLatestFullBackupAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupCatalogEntry>> GetAllBackupsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupCatalogEntry>> GetBackupChainAsync(string targetBackupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupCatalogEntry>> GetDependentIncrementalsAsync(string fullBackupId, CancellationToken cancellationToken = default);

    Task<int> GetIncrementalCountSinceLastFullAsync(CancellationToken cancellationToken = default);

    Task DeleteBackupRecordAsync(string backupId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages repository physical directory structure, atomic promotions, and file paths.
/// </summary>
public interface IBackupStorage
{
    string RootPath { get; }

    string GetCatalogPath();

    string GenerateBackupFilePath(string backupId, Core.Enums.BackupType type, DateTime dateUtc);

    string GetTempFilePath(string finalBackupFilePath);

    void CommitTempFile(string tempFilePath, string finalFilePath);

    void CleanupTempFiles();

    void DeleteFile(string filePath);

    bool FileExists(string filePath);

    long GetAvailableFreeSpaceBytes();
}

/// <summary>
/// Mutual exclusion lock preventing concurrent backups on the same repository.
/// </summary>
public interface IRepositoryLock : IDisposable, IAsyncDisposable
{
    bool IsAcquired { get; }
}
