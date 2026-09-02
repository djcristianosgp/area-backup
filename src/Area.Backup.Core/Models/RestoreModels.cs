namespace Area.Backup.Core.Models;

/// <summary>
/// Options for restoring files and database dumps from a backup point.
/// </summary>
public sealed class RestoreOptions
{
    /// <summary>
    /// Base destination directory where files will be restored.
    /// </summary>
    public string DestinationPath { get; set; } = string.Empty;

    /// <summary>
    /// Optional filter to restore only a specific source ID. If null, all sources are restored.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Optional relative path prefix filter (e.g. "Bitmaps\").
    /// </summary>
    public string? RelativePathFilter { get; set; }

    /// <summary>
    /// Whether to restore files into their original full paths (e.g. C:\ERP, D:\Docs) instead of DestinationPath.
    /// </summary>
    public bool RestoreToOriginalLocations { get; set; } = false;

    /// <summary>
    /// Whether existing files in the destination should be overwritten.
    /// </summary>
    public bool OverwriteExisting { get; set; } = true;

    /// <summary>
    /// Whether to verify SHA-256 checksums of restored files against manifest after extraction.
    /// </summary>
    public bool VerifyChecksumsAfterRestore { get; set; } = true;

    /// <summary>
    /// Whether to delete files that were marked as deleted in incremental chain points.
    /// </summary>
    public bool ApplyDeletions { get; set; } = true;
}

/// <summary>
/// Result of a completed restore operation.
/// </summary>
public sealed class RestoreResult
{
    public bool Success { get; init; }
    public string TargetBackupId { get; init; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
    public int BackupsInChainCount { get; init; }
    public long FilesRestored { get; init; }
    public long FilesOverwritten { get; init; }
    public long FilesDeleted { get; init; }
    public long BytesRestored { get; init; }
    public TimeSpan Duration { get; init; }
    public List<BackupWarning> Warnings { get; init; } = new();
    public List<BackupError> Errors { get; init; } = new();
}
