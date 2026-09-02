using Area.Backup.Core.Exceptions;
using Area.Backup.Core.Interfaces;

namespace Area.Backup.Infrastructure.Storage;

/// <summary>
/// Prevents concurrent backup operations on the same repository directory using a non-reentrant lock file stream.
/// </summary>
public sealed class RepositoryLock : IRepositoryLock
{
    private readonly string _lockFilePath;
    private FileStream? _lockStream;
    private bool _disposed;

    public bool IsAcquired => _lockStream != null && !_disposed;

    private RepositoryLock(string lockFilePath, FileStream lockStream)
    {
        _lockFilePath = lockFilePath;
        _lockStream = lockStream;
    }

    public static RepositoryLock Acquire(string repositoryRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRootPath);

        if (!Directory.Exists(repositoryRootPath))
        {
            Directory.CreateDirectory(repositoryRootPath);
        }

        var lockFilePath = Path.Combine(repositoryRootPath, ".backup.lock");

        try
        {
            var stream = new FileStream(
                lockFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);

            return new RepositoryLock(lockFilePath, stream);
        }
        catch (IOException)
        {
            throw new BackupAlreadyRunningException(repositoryRootPath);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_lockStream != null)
        {
            _lockStream.Dispose();
            _lockStream = null;
        }

        try
        {
            if (File.Exists(_lockFilePath))
            {
                File.Delete(_lockFilePath);
            }
        }
        catch
        {
            // Delete on close might have removed it already
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
