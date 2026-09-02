using Area.Backup.Core.Enums;

namespace Area.Backup.Core.Models;

/// <summary>
/// Represents a single backup record stored in the persistent catalog database.
/// </summary>
public sealed class BackupCatalogEntry
{
    public string BackupId { get; set; } = string.Empty;
    public BackupType Type { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public BackupStatus Status { get; set; } = BackupStatus.Completed;
    public string? ParentBackupId { get; set; }
    public string? RootFullBackupId { get; set; }
    public string RelativeFilePath { get; set; } = string.Empty; // e.g. "2026/09/20260902-143000-full.backup"
    public long FileCount { get; set; }
    public long DeletedFileCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public long CompressedSizeBytes { get; set; }
    public string? ChecksumSha256 { get; set; }
    public string EngineVersion { get; set; } = "1.0.0";
    public int FormatVersion { get; set; } = 1;
}

/// <summary>
/// Represents an individual file version record stored in the catalog.
/// </summary>
public sealed class BackupFileRecord
{
    public long Id { get; set; }
    public string BackupId { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
    public string? ChecksumSha256 { get; set; }
    public string StoredInBackupId { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

/// <summary>
/// High-level catalog container returned to clients.
/// </summary>
public sealed class BackupCatalog
{
    public string RepositoryPath { get; init; } = string.Empty;
    public List<BackupCatalogEntry> Entries { get; init; } = new();
    public long TotalSizeBytes => Entries.Sum(e => e.CompressedSizeBytes);
    public int TotalBackups => Entries.Count;
    public int FullBackupsCount => Entries.Count(e => e.Type == BackupType.Full);
    public int IncrementalBackupsCount => Entries.Count(e => e.Type == BackupType.Incremental);
}
