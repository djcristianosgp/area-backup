namespace Area.Backup.Core.Enums;

/// <summary>
/// Status of an individual file detected during backup change evaluation.
/// </summary>
public enum FileChangeType
{
    None = 0,
    Added = 1,
    Modified = 2,
    Deleted = 3
}
