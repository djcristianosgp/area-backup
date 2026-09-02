namespace Atual.Backup.Core.Enums;

/// <summary>
/// Represents the distinct stages of a backup execution workflow.
/// </summary>
public enum BackupStage
{
    Initializing = 0,
    Scanning = 1,
    DetectingChanges = 2,
    Reading = 3,
    Compressing = 4,
    Writing = 5,
    Validating = 6,
    Finalizing = 7,
    Completed = 8
}
