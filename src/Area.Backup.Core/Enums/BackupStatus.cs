namespace Area.Backup.Core.Enums;

/// <summary>
/// Status of the backup engine.
/// </summary>
public enum BackupStatus
{
    Idle = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Validating = 5,
    Restoring = 6
}
