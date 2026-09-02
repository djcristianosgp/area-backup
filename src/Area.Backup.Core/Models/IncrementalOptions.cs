namespace Area.Backup.Core.Models;

/// <summary>
/// Settings governing incremental change detection and Auto mode behavior.
/// </summary>
public sealed class IncrementalOptions
{
    /// <summary>
    /// Whether incremental backup capabilities are enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Maximum number of sequential incremental backups before forcing a new Full backup in Auto mode.
    /// Default is 7.
    /// </summary>
    public int MaxIncrementalBackups { get; init; } = 7;

    /// <summary>
    /// Maximum elapsed days since the parent Full backup before forcing a new Full backup in Auto mode.
    /// Default is 7 days.
    /// </summary>
    public int MaxDaysSinceFull { get; init; } = 7;

    /// <summary>
    /// Whether to attempt using the NTFS USN Journal for ultra-fast change detection when running on NTFS volumes.
    /// If unsupported or unavailable, automatically falls back to full filesystem scan.
    /// </summary>
    public bool UseUsnJournal { get; init; } = true;

    /// <summary>
    /// Whether to compute and compare cryptographic SHA-256 hashes during change detection for candidates.
    /// </summary>
    public bool UseHashValidation { get; init; } = true;
}
