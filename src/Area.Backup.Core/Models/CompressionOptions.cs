using Area.Backup.Core.Enums;

namespace Area.Backup.Core.Models;

/// <summary>
/// Settings for package compression.
/// </summary>
public sealed class CompressionOptions
{
    public CompressionAlgorithm Algorithm { get; init; } = CompressionAlgorithm.Zip;
    public CompressionLevel Level { get; init; } = CompressionLevel.Optimal;

    /// <summary>
    /// Whether file-level deduplication is enabled across sources with identical SHA-256 hashes.
    /// </summary>
    public bool DeduplicationEnabled { get; init; } = false;
}
