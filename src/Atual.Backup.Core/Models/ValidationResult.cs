using Atual.Backup.Core.Enums;

namespace Atual.Backup.Core.Models;

/// <summary>
/// Result of an integrity validation operation on a backup file or chain.
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public string BackupPath { get; init; } = string.Empty;
    public string BackupId { get; init; } = string.Empty;
    public ValidationMode Mode { get; init; }
    public long ExpectedFiles { get; init; }
    public long ValidFiles { get; init; }
    public long InvalidFiles { get; init; }
    public long MissingFiles { get; init; }
    public long InvalidChecksums { get; init; }
    public bool RestoreTested { get; init; }
    public bool DependencyChainValid { get; init; } = true;
    public TimeSpan Duration { get; init; }
    public List<string> ValidationErrors { get; init; } = new();
    public List<string> ValidationWarnings { get; init; } = new();
}
