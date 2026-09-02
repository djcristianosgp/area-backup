using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;
using Area.Backup.Core.Models.Manifest;
using Area.Backup.Infrastructure.Catalog;
using Area.Backup.Infrastructure.Retention;
using Area.Backup.Infrastructure.Storage;
using Xunit;

namespace Area.Backup.UnitTests;

public class RetentionPolicyServiceTests : IDisposable
{
    private readonly string _repoDir;

    public RetentionPolicyServiceTests()
    {
        _repoDir = Path.Combine(Path.GetTempPath(), $"RetentionTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_repoDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_repoDir, true); } catch { }
    }

    [Fact]
    public async Task Should_Protect_Full_Backup_If_Dependent_Incremental_Is_Still_Active()
    {
        var storage = new LocalFileSystemStorage(_repoDir);
        using var catalog = new SqliteCatalogRepository(storage.GetCatalogPath());
        await catalog.InitializeAsync();

        var baseTime = DateTime.UtcNow.AddDays(-40); // 40 days old

        // Full 1 (Old)
        var full1File = storage.GenerateBackupFilePath("FULL1", BackupType.Full, baseTime);
        File.WriteAllText(full1File, "mock full 1");
        var full1Entry = new BackupCatalogEntry
        {
            BackupId = "FULL1",
            Type = BackupType.Full,
            CreatedAtUtc = baseTime,
            RelativeFilePath = Path.GetRelativePath(_repoDir, full1File),
            CompressedSizeBytes = 1000
        };
        await catalog.RegisterBackupAsync(full1Entry, new BackupManifest { BackupId = "FULL1", Type = BackupType.Full, CreatedAtUtc = baseTime });

        // Inc 1 (Depends on Full 1, created 2 days ago -> Active)
        var inc1Time = DateTime.UtcNow.AddDays(-2);
        var inc1File = storage.GenerateBackupFilePath("INC1", BackupType.Incremental, inc1Time);
        File.WriteAllText(inc1File, "mock inc 1");
        var inc1Entry = new BackupCatalogEntry
        {
            BackupId = "INC1",
            Type = BackupType.Incremental,
            CreatedAtUtc = inc1Time,
            ParentBackupId = "FULL1",
            RootFullBackupId = "FULL1",
            RelativeFilePath = Path.GetRelativePath(_repoDir, inc1File),
            CompressedSizeBytes = 200
        };
        await catalog.RegisterBackupAsync(inc1Entry, new BackupManifest { BackupId = "INC1", Type = BackupType.Incremental, CreatedAtUtc = inc1Time, ParentBackupId = "FULL1", RootFullBackupId = "FULL1" });

        var service = new RetentionPolicyService();
        var policy = new RetentionPolicy
        {
            Enabled = true,
            MaxDays = 30, // Cutoff: 30 days. Full 1 is 40 days old, but Inc 1 is 2 days old!
            KeepFullBackups = 1
        };

        var result = await service.ApplyRetentionAsync(catalog, storage, policy);

        // Invariant check: FULL1 MUST NOT be removed because INC1 depends on it!
        Assert.DoesNotContain("FULL1", result.RemovedBackupIds);
        Assert.True(File.Exists(full1File));
        Assert.True(File.Exists(inc1File));
    }
}
