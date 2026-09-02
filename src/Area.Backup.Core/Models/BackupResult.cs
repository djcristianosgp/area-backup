using Area.Backup.Core.Enums;

namespace Area.Backup.Core.Models;

/// <summary>
/// Detailed result of a completed backup execution.
/// </summary>
public sealed class BackupResult
{
    public bool Success { get; init; }
    public string BackupId { get; init; } = string.Empty;
    public BackupType Type { get; init; }
    public string? ParentBackupId { get; init; }
    public string? BackupPath { get; init; }
    public DateTime CreatedAtUtc { get; init; }

    public long FilesScanned { get; init; }
    public long FilesAdded { get; init; }
    public long FilesModified { get; init; }
    public long FilesDeleted { get; init; }
    public long FilesSkipped { get; init; }

    public long BytesScanned { get; init; }
    public long BytesBackedUp { get; init; }
    public long CompressedSize { get; init; }

    public TimeSpan Duration { get; init; }
    public bool IntegrityValidated { get; init; }

    public List<BackupWarning> Warnings { get; init; } = new();
    public List<BackupError> Errors { get; init; } = new();

    public static BackupResult Failed(string backupId, BackupType type, TimeSpan duration, IEnumerable<BackupError> errors) =>
        new()
        {
            Success = false,
            BackupId = backupId,
            Type = type,
            Duration = duration,
            Errors = errors.ToList()
        };
}
