using Area.Backup.Core.Enums;

namespace Area.Backup.Core.Models;

/// <summary>
/// Root configuration object defining a backup job.
/// </summary>
public sealed class BackupConfiguration
{
    /// <summary>
    /// Target repository path where backup archives and catalog are stored (e.g. @"C:\Backup\Atual").
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Backup execution type (Auto, Full, Incremental). Default: Auto.
    /// </summary>
    public BackupType BackupType { get; set; } = BackupType.Auto;

    /// <summary>
    /// List of source folders/shares to be backed up.
    /// </summary>
    public List<BackupSource> Sources { get; set; } = new();

    /// <summary>
    /// File, directory, extension, and pattern exclusions.
    /// </summary>
    public List<BackupExclusion> Exclusions { get; set; } = new();

    /// <summary>
    /// Incremental backup settings.
    /// </summary>
    public IncrementalOptions Incremental { get; set; } = new();

    /// <summary>
    /// Package compression settings.
    /// </summary>
    public CompressionOptions Compression { get; set; } = new();

    /// <summary>
    /// Automated retention policy settings.
    /// </summary>
    public RetentionPolicy Retention { get; set; } = new();

    /// <summary>
    /// Integrity validation settings.
    /// </summary>
    public ValidationOptions Validation { get; set; } = new();

    /// <summary>
    /// Concurrency and buffer performance tuning.
    /// </summary>
    public PerformanceOptions Performance { get; set; } = new();

    /// <summary>
    /// Database backup settings.
    /// </summary>
    public DatabaseBackupOptions Database { get; set; } = new();

    /// <summary>
    /// Validates the configuration and throws <see cref="Area.Backup.Core.Exceptions.BackupConfigurationException"/> if invalid.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RepositoryPath))
            throw new Exceptions.BackupConfigurationException("RepositoryPath is required.");

        if (Sources.Count == 0 && !Database.Enabled)
            throw new Exceptions.BackupConfigurationException("At least one BackupSource or an enabled Database configuration is required.");

        var duplicateSourceIds = Sources.GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
                                        .Where(g => g.Count() > 1)
                                        .Select(g => g.Key)
                                        .ToList();

        if (duplicateSourceIds.Count > 0)
            throw new Exceptions.BackupConfigurationException($"Duplicate Source IDs detected: {string.Join(", ", duplicateSourceIds)}");
    }
}
