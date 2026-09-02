namespace Area.Backup.Core.Exceptions;

/// <summary>
/// Base exception for all Area Backup Engine operations.
/// </summary>
public class BackupException : Exception
{
    public BackupException(string message) : base(message) { }
    public BackupException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a backup operation is requested on a repository where another operation is already active.
/// </summary>
public sealed class BackupAlreadyRunningException : BackupException
{
    public string RepositoryPath { get; }

    public BackupAlreadyRunningException(string repositoryPath)
        : base($"Another backup operation is currently running on repository: '{repositoryPath}'.")
    {
        RepositoryPath = repositoryPath;
    }
}

/// <summary>
/// Thrown when backup parameters, paths or options are invalid.
/// </summary>
public sealed class BackupConfigurationException : BackupException
{
    public BackupConfigurationException(string message) : base(message) { }
    public BackupConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when archive integrity check fails (e.g. SHA-256 hash mismatch or corrupted package).
/// </summary>
public sealed class BackupIntegrityException : BackupException
{
    public string? BackupPath { get; }
    public string? CorruptedFile { get; }

    public BackupIntegrityException(string message, string? backupPath = null, string? corruptedFile = null)
        : base(message)
    {
        BackupPath = backupPath;
        CorruptedFile = corruptedFile;
    }

    public BackupIntegrityException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a restore operation encounters an error.
/// </summary>
public sealed class BackupRestoreException : BackupException
{
    public BackupRestoreException(string message) : base(message) { }
    public BackupRestoreException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when repository storage operations (disk full, access denied, invalid structure) fail.
/// </summary>
public sealed class BackupStorageException : BackupException
{
    public BackupStorageException(string message) : base(message) { }
    public BackupStorageException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a database backup provider encounters an execution or connection error.
/// </summary>
public sealed class DatabaseBackupException : BackupException
{
    public DatabaseBackupException(string message) : base(message) { }
    public DatabaseBackupException(string message, Exception innerException) : base(message, innerException) { }
}
