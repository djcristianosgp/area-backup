using Atual.Backup.Core.Models;

namespace Atual.Backup.Core.Interfaces;

/// <summary>
/// Result of a database backup operation.
/// </summary>
public sealed class DatabaseBackupResult
{
    public bool Success { get; init; }
    public string OutputFilePath { get; init; } = string.Empty;
    public string EntryNameInArchive { get; init; } = "database/dump.fbk";
    public long SizeBytes { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Provider abstraction for backing up transactional databases without copying active in-use files.
/// </summary>
public interface IDatabaseBackupProvider
{
    string ProviderName { get; }
    bool CanHandle(DatabaseBackupOptions options);
    Task<DatabaseBackupResult> BackupAsync(
        DatabaseBackupOptions options,
        string temporaryWorkingDirectory,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Enforces backup retention policies while safeguarding dependent incremental chains.
/// </summary>
public interface IRetentionPolicyService
{
    Task<RetentionExecutionResult> ApplyRetentionAsync(
        ICatalogRepository catalog,
        IBackupStorage storage,
        RetentionPolicy policy,
        CancellationToken cancellationToken = default);
}

public sealed class RetentionExecutionResult
{
    public int FullBackupsRemoved { get; set; }
    public int IncrementalBackupsRemoved { get; set; }
    public long BytesFreed { get; set; }
    public List<string> RemovedBackupIds { get; set; } = new();
    public List<string> RetainedBackupIds { get; set; } = new();
}

/// <summary>
/// Validates package integrity, checksums, and dependency chains.
/// </summary>
public interface IIntegrityValidator
{
    Task<ValidationResult> ValidateAsync(
        string backupPath,
        ValidationOptions options,
        ICatalogRepository? catalog = null,
        IBackupStorage? storage = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Reconstructs filesystem state from Full and Incremental backup chains.
/// </summary>
public interface IRestoreEngine
{
    Task<RestoreResult> RestoreAsync(
        string targetBackupPath,
        RestoreOptions options,
        ICatalogRepository catalog,
        IBackupStorage storage,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
