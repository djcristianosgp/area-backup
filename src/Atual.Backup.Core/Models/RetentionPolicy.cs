namespace Atual.Backup.Core.Models;

/// <summary>
/// Retention rules for automated backup cleanup.
/// Invariant: A Full backup is NEVER purged if active Incremental backups still depend on it.
/// </summary>
public sealed class RetentionPolicy
{
    /// <summary>
    /// Whether automatic retention cleanup is enabled after backup execution.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Number of most recent Full backups (and their dependent chains) to keep. Default: 4.
    /// </summary>
    public int KeepFullBackups { get; init; } = 4;

    /// <summary>
    /// Maximum number of Incremental backups to keep. Default: 30.
    /// </summary>
    public int KeepIncrementalBackups { get; init; } = 30;

    /// <summary>
    /// Maximum age in days for retaining backups. 0 means unlimited by age. Default: 30 days.
    /// </summary>
    public int MaxDays { get; init; } = 30;

    /// <summary>
    /// Maximum storage quota in bytes. 0 means unlimited.
    /// </summary>
    public long MaxStorageSizeBytes { get; init; } = 0;
}
