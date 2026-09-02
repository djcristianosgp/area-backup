namespace Atual.Backup.Core.Enums;

/// <summary>
/// Defines the backup execution type.
/// </summary>
public enum BackupType
{
    /// <summary>
    /// Automatically decides between Full and Incremental based on catalog history, retention and policies.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Creates a complete standalone recovery point with all configured sources and databases.
    /// </summary>
    Full = 1,

    /// <summary>
    /// Creates a delta backup containing only new, modified, and deleted files since the last backup.
    /// </summary>
    Incremental = 2
}
