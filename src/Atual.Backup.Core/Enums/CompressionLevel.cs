namespace Atual.Backup.Core.Enums;

/// <summary>
/// Desired trade-off between speed and compression ratio.
/// </summary>
public enum CompressionLevel
{
    Optimal = 0,
    Fastest = 1,
    NoCompression = 2,
    SmallestSize = 3
}
