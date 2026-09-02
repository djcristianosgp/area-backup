namespace Area.Backup.Core.Models;

/// <summary>
/// Type of backup exclusion rule.
/// </summary>
public enum ExclusionType
{
    Pattern = 0,
    DirectoryName = 1,
    Extension = 2,
    ExactPath = 3
}

/// <summary>
/// Defines an exclusion rule for paths, directories or file patterns.
/// </summary>
public sealed class BackupExclusion
{
    public string Pattern { get; init; }
    public ExclusionType Type { get; init; }

    public BackupExclusion(string pattern, ExclusionType type = ExclusionType.Pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        Pattern = pattern.Trim();
        Type = type;
    }

    public BackupExclusion()
    {
        Pattern = string.Empty;
        Type = ExclusionType.Pattern;
    }

    public static BackupExclusion FromPattern(string pattern) => new(pattern, ExclusionType.Pattern);
    public static BackupExclusion FromDirectory(string directoryName) => new(directoryName, ExclusionType.DirectoryName);
    public static BackupExclusion FromExtension(string extension) =>
        new(extension.StartsWith('.') ? extension : $".{extension}", ExclusionType.Extension);
    public static BackupExclusion FromPath(string exactPath) => new(exactPath, ExclusionType.ExactPath);

    public override string ToString() => $"Exclusion [{Type}]: {Pattern}";
}
