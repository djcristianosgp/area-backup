namespace Area.Backup.Core.Models;

/// <summary>
/// Execution parameters for concurrency, buffer sizes, and I/O limits.
/// </summary>
public sealed class PerformanceOptions
{
    /// <summary>
    /// Maximum degree of parallelism for scanning and hashing. Defaults to ProcessorCount clamped between 1 and 16.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = Math.Clamp(Environment.ProcessorCount, 1, 16);

    /// <summary>
    /// Stream buffer size in bytes for reading, hashing, and compression. Default is 64 KB (65536).
    /// </summary>
    public int BufferSize { get; init; } = 64 * 1024;

    /// <summary>
    /// Whether to enable multi-threaded directory scanning.
    /// </summary>
    public bool EnableParallelScanning { get; init; } = true;

    /// <summary>
    /// Whether to compute candidate file hashes in parallel.
    /// </summary>
    public bool EnableParallelHashing { get; init; } = true;

    /// <summary>
    /// Whether a locked/in-use file aborts the entire backup or logs a warning and proceeds with remaining files.
    /// </summary>
    public bool FailOnLockedFile { get; init; } = false;
}
