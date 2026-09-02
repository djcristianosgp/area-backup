using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Models;
using Atual.Backup.Core.Models.Manifest;
using Atual.Backup.Infrastructure.Catalog;
using Xunit;

namespace Atual.Backup.UnitTests;

public class SqliteCatalogRepositoryTests : IDisposable
{
    private readonly string _testDbPath;

    public SqliteCatalogRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"CatalogTest_{Guid.NewGuid():N}", "catalog.db");
    }

    public void Dispose()
    {
        try
        {
            var dir = Path.GetDirectoryName(_testDbPath);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch { }
    }

    [Fact]
    public async Task Should_Register_And_Retrieve_Backup_Entries_And_Chains()
    {
        using var catalog = new SqliteCatalogRepository(_testDbPath);
        await catalog.InitializeAsync();

        // 1. Register Full Backup
        var fullEntry = new BackupCatalogEntry
        {
            BackupId = "20260901-100000",
            Type = BackupType.Full,
            CreatedAtUtc = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            Status = BackupStatus.Completed,
            RelativeFilePath = "2026/09/20260901-100000-full.backup",
            FileCount = 10,
            CompressedSizeBytes = 5000
        };
        var fullManifest = new BackupManifest
        {
            BackupId = fullEntry.BackupId,
            Type = BackupType.Full,
            CreatedAtUtc = fullEntry.CreatedAtUtc,
            Sources = [new ManifestSource { SourceId = "SRC_1", OriginalPath = @"C:\ERP" }],
            Files = [new ManifestFileEntry { SourceId = "SRC_1", RelativePath = "db.fdb", Size = 1000, Sha256 = "hashA", ArchiveEntryPath = "files/SRC_1/db.fdb" }]
        };
        await catalog.RegisterBackupAsync(fullEntry, fullManifest);

        // 2. Register Incremental Backup
        var incEntry = new BackupCatalogEntry
        {
            BackupId = "20260902-100000",
            Type = BackupType.Incremental,
            CreatedAtUtc = new DateTime(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc),
            ParentBackupId = "20260901-100000",
            RootFullBackupId = "20260901-100000",
            Status = BackupStatus.Completed,
            RelativeFilePath = "2026/09/20260902-100000-incremental.backup",
            FileCount = 2,
            CompressedSizeBytes = 1000
        };
        var incManifest = new BackupManifest
        {
            BackupId = incEntry.BackupId,
            Type = BackupType.Incremental,
            CreatedAtUtc = incEntry.CreatedAtUtc,
            ParentBackupId = incEntry.ParentBackupId,
            RootFullBackupId = incEntry.RootFullBackupId,
            Sources = fullManifest.Sources,
            Files = [new ManifestFileEntry { SourceId = "SRC_1", RelativePath = "new.txt", Size = 500, Sha256 = "hashB", ArchiveEntryPath = "files/SRC_1/new.txt" }]
        };
        await catalog.RegisterBackupAsync(incEntry, incManifest);

        // 3. Verify Latest Backups
        var latest = await catalog.GetLatestBackupAsync();
        Assert.NotNull(latest);
        Assert.Equal("20260902-100000", latest.BackupId);

        var latestFull = await catalog.GetLatestFullBackupAsync();
        Assert.NotNull(latestFull);
        Assert.Equal("20260901-100000", latestFull.BackupId);

        // 4. Verify Chain Resolution
        var chain = await catalog.GetBackupChainAsync("20260902-100000");
        Assert.Equal(2, chain.Count);
        Assert.Equal("20260901-100000", chain[0].BackupId);
        Assert.Equal("20260902-100000", chain[1].BackupId);

        // 5. Verify Dependent Incrementals
        var dependents = await catalog.GetDependentIncrementalsAsync("20260901-100000");
        Assert.Single(dependents);
        Assert.Equal("20260902-100000", dependents[0].BackupId);
    }
}
