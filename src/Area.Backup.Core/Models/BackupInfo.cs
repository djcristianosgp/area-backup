using Area.Backup.Core.Enums;

namespace Area.Backup.Core.Models;

/// <summary>
/// Summary information extracted from an existing backup archive or catalog entry.
/// </summary>
public sealed class BackupInfo
{
    public string BackupId { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public BackupType Type { get; init; }
    public string? ParentBackupId { get; init; }
    public string? RootFullBackupId { get; init; }
    public long FileCount { get; init; }
    public long DeletedFileCount { get; init; }
    public long TotalSizeBytes { get; init; }
    public long CompressedSizeBytes { get; init; }
    public string BackupPath { get; init; } = string.Empty;
    public string EngineVersion { get; init; } = string.Empty;
    public int FormatVersion { get; init; }
    public bool IsDatabaseIncluded { get; init; }
    public List<string> Sources { get; init; } = new();
}
