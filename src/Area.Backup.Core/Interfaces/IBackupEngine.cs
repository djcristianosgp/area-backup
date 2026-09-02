using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;

namespace Area.Backup.Core.Interfaces;

/// <summary>
/// Main public facade interface for Area Backup Engine.
/// </summary>
public interface IBackupEngine
{
    /// <summary>
    /// Current execution status of the engine.
    /// </summary>
    BackupStatus Status { get; }

    /// <summary>
    /// Raised whenever backup or restore progress advances.
    /// </summary>
    event EventHandler<BackupProgress>? ProgressChanged;

    /// <summary>
    /// Raised when execution moves to a new pipeline stage.
    /// </summary>
    event EventHandler<BackupStage>? StageChanged;

    /// <summary>
    /// Raised when a backup or restore job finishes successfully.
    /// </summary>
    event EventHandler<BackupResult>? Completed;

    /// <summary>
    /// Raised when a non-fatal warning or fatal error occurs.
    /// </summary>
    event EventHandler<BackupError>? Error;

    /// <summary>
    /// Creates a Full or Incremental backup according to the provided configuration.
    /// </summary>
    Task<BackupResult> CreateBackupAsync(
        BackupConfiguration configuration,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the physical and cryptographic integrity of a backup package and its dependency chain.
    /// </summary>
    Task<ValidationResult> ValidateBackupAsync(
        string backupPath,
        ValidationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores files and databases from a backup point in time.
    /// </summary>
    Task<RestoreResult> RestoreBackupAsync(
        string backupPath,
        RestoreOptions options,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads and inspects header and manifest information from a backup package without performing full extraction.
    /// </summary>
    BackupInfo GetBackupInfo(string backupPath);

    /// <summary>
    /// Retrieves the persistent backup catalog for the specified repository path.
    /// </summary>
    Task<BackupCatalog> GetCatalogAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default);
}
