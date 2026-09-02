using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Exceptions;
using Atual.Backup.Core.Models;
using Atual.Backup.Infrastructure.Storage;
using Xunit;

namespace Atual.Backup.IntegrationTests;

public class ConcurrencyLockIntegrationTests : IDisposable
{
    private readonly string _testBase;
    private readonly string _sourceDir;
    private readonly string _repoDir;

    public ConcurrencyLockIntegrationTests()
    {
        _testBase = Path.Combine(Path.GetTempPath(), $"ConcurrencyTest_{Guid.NewGuid():N}");
        _sourceDir = Path.Combine(_testBase, "Source");
        _repoDir = Path.Combine(_testBase, "Repo");

        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_repoDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testBase, true); } catch { }
    }

    [Fact]
    public async Task Should_Prevent_Concurrent_Backup_Operations_On_Same_Repository()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "test.txt"), "sample data");

        var config = new BackupConfiguration
        {
            RepositoryPath = _repoDir,
            BackupType = BackupType.Full,
            Sources = [new BackupSource(_sourceDir, "SRC_1")]
        };

        // Manually acquire lock simulating a running background backup job
        using var activeLock = RepositoryLock.Acquire(_repoDir);
        Assert.True(activeLock.IsAcquired);

        var engine = new BackupEngine();

        // Attempting a second backup on the locked repo should throw BackupAlreadyRunningException
        await Assert.ThrowsAsync<BackupAlreadyRunningException>(() => engine.CreateBackupAsync(config));
    }
}
