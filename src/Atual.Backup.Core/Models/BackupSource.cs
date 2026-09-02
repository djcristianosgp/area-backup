namespace Atual.Backup.Core.Models;

/// <summary>
/// Represents a configured backup source directory or network share.
/// </summary>
public sealed class BackupSource
{
    /// <summary>
    /// Unique and stable identifier for this source (e.g. "SRC_ERP", "SRC_DOCS").
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Original root path (e.g. "C:\ERP", "D:\Documentos\ERP", "\\SERVIDOR\Compartilhado").
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Optional human-readable description for UI / logs.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether this source is enabled for backup.
    /// </summary>
    public bool Enabled { get; init; } = true;

    public BackupSource(string path, string? id = null, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = System.IO.Path.GetFullPath(path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
        Id = string.IsNullOrWhiteSpace(id) ? GenerateStableId(Path) : id.Trim();
        Description = description;
    }

    // Required for deserialization
    public BackupSource()
    {
        Id = string.Empty;
        Path = string.Empty;
    }

    private static string GenerateStableId(string path)
    {
        var sanitized = path.Replace(':', '_').Replace('\\', '_').Replace('/', '_').Trim('_');
        return $"SRC_{sanitized.ToUpperInvariant()}";
    }

    public override string ToString() => $"[{Id}] {Path}";
}
