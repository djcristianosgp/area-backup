using System.Text.Json.Serialization;
using Area.Backup.Core.Enums;

namespace Area.Backup.Core.Models.Manifest;

public sealed class ManifestSource
{
    [JsonPropertyName("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [JsonPropertyName("originalPath")]
    public string OriginalPath { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class ManifestFileEntry
{
    [JsonPropertyName("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [JsonPropertyName("relativePath")]
    public string RelativePath { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("lastWriteTimeUtc")]
    public DateTime LastWriteTimeUtc { get; set; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("archiveEntryPath")]
    public string ArchiveEntryPath { get; set; } = string.Empty;

    [JsonPropertyName("changeType")]
    public FileChangeType ChangeType { get; set; } = FileChangeType.Added;
}

public sealed class ManifestDeletedFile
{
    [JsonPropertyName("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [JsonPropertyName("relativePath")]
    public string RelativePath { get; set; } = string.Empty;

    [JsonPropertyName("deletedAtUtc")]
    public DateTime DeletedAtUtc { get; set; }
}

public sealed class BackupManifest
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; } = 1;

    [JsonPropertyName("engineVersion")]
    public string EngineVersion { get; set; } = "1.0.0";

    [JsonPropertyName("backupId")]
    public string BackupId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public BackupType Type { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("parentBackupId")]
    public string? ParentBackupId { get; set; }

    [JsonPropertyName("rootFullBackupId")]
    public string? RootFullBackupId { get; set; }

    [JsonPropertyName("sources")]
    public List<ManifestSource> Sources { get; set; } = new();

    [JsonPropertyName("files")]
    public List<ManifestFileEntry> Files { get; set; } = new();

    [JsonPropertyName("deletedFiles")]
    public List<ManifestDeletedFile> DeletedFiles { get; set; } = new();

    [JsonPropertyName("databaseDumpEntryPath")]
    public string? DatabaseDumpEntryPath { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}
