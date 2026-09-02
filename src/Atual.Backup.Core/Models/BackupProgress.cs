using Atual.Backup.Core.Enums;

namespace Atual.Backup.Core.Models;

/// <summary>
/// Real-time progress state reported during backup and restore workflows.
/// </summary>
public sealed class BackupProgress
{
    public BackupStage Stage { get; init; } = BackupStage.Initializing;
    public long FilesTotal { get; init; }
    public long FilesProcessed { get; init; }
    public long BytesTotal { get; init; }
    public long BytesProcessed { get; init; }
    public double Percentage { get; init; }
    public string? CurrentFile { get; init; }
    public TimeSpan Elapsed { get; init; }
    public TimeSpan? EstimatedRemaining { get; init; }
    public double SpeedBytesPerSecond { get; init; }

    public static BackupProgress Create(
        BackupStage stage,
        long filesProcessed,
        long filesTotal,
        long bytesProcessed,
        long bytesTotal,
        string? currentFile,
        TimeSpan elapsed)
    {
        double percentage = 0;
        if (bytesTotal > 0)
        {
            percentage = Math.Clamp((double)bytesProcessed / bytesTotal * 100.0, 0.0, 100.0);
        }
        else if (filesTotal > 0)
        {
            percentage = Math.Clamp((double)filesProcessed / filesTotal * 100.0, 0.0, 100.0);
        }

        double speed = elapsed.TotalSeconds > 0.5 ? (bytesProcessed / elapsed.TotalSeconds) : 0;

        TimeSpan? remaining = null;
        if (speed > 0 && bytesTotal > bytesProcessed)
        {
            long remainingBytes = bytesTotal - bytesProcessed;
            remaining = TimeSpan.FromSeconds(remainingBytes / speed);
        }

        return new BackupProgress
        {
            Stage = stage,
            FilesTotal = filesTotal,
            FilesProcessed = filesProcessed,
            BytesTotal = bytesTotal,
            BytesProcessed = bytesProcessed,
            Percentage = percentage,
            CurrentFile = currentFile,
            Elapsed = elapsed,
            EstimatedRemaining = remaining,
            SpeedBytesPerSecond = speed
        };
    }
}
