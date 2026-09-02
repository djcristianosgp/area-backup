using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;
using Xunit;

namespace Area.Backup.IntegrationTests;

public class FullAndIncrementalBackupIntegrationTests : IDisposable
{
    private readonly string _testBase;
    private readonly string _sourceDir;
    private readonly string _repoDir;

    public FullAndIncrementalBackupIntegrationTests()
    {
        _testBase = Path.Combine(Path.GetTempPath(), $"AtualIntegTest_{Guid.NewGuid():N}");
        _sourceDir = Path.Combine(_testBase, "ERP_Source");
        _repoDir = Path.Combine(_testBase, "Repository");

        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_repoDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testBase, true); } catch { }
    }

    [Fact]
    public async Task Should_Execute_Full_Then_Incremental_Processing_Only_Deltas()
    {
        var engine = new BackupEngine();

        // 1. Setup initial source files
        var file1 = Path.Combine(_sourceDir, "system.ini");
        var file2 = Path.Combine(_sourceDir, "data.db");
        await File.WriteAllTextAsync(file1, "Initial Config");
        await File.WriteAllTextAsync(file2, "Database record 1234567890");

        var config = new BackupConfiguration
        {
            RepositoryPath = _repoDir,
            BackupType = BackupType.Full,
            Sources = [new BackupSource(_sourceDir, "ERP_SRC")]
        };

        // --- EXECUTE FULL BACKUP ---
        var fullResult = await engine.CreateBackupAsync(config);

        Assert.True(fullResult.Success);
        Assert.Equal(BackupType.Full, fullResult.Type);
        Assert.Equal(2, fullResult.FilesScanned);
        Assert.Equal(2, fullResult.FilesAdded);
        Assert.True(File.Exists(fullResult.BackupPath));

        // 2. Modify state:
        // - system.ini is UNCHANGED
        // - data.db is MODIFIED
        // - new_report.pdf is ADDED
        // - file_to_delete.tmp is created and immediately deleted
        await Task.Delay(1100); // Ensure timestamp advances cleanly
        await File.WriteAllTextAsync(file2, "Database record 1234567890 - UPDATED MODIFICATION");

        var file3 = Path.Combine(_sourceDir, "new_report.pdf");
        await File.WriteAllTextAsync(file3, "Financial Report PDF Content");

        // --- EXECUTE INCREMENTAL BACKUP ---
        config.BackupType = BackupType.Incremental;
        var incResult = await engine.CreateBackupAsync(config);

        Assert.True(incResult.Success);
        Assert.Equal(BackupType.Incremental, incResult.Type);
        Assert.Equal(fullResult.BackupId, incResult.ParentBackupId);
        Assert.Equal(3, incResult.FilesScanned);
        Assert.Equal(1, incResult.FilesAdded); // new_report.pdf
        Assert.Equal(1, incResult.FilesModified); // data.db
        Assert.Equal(0, incResult.FilesDeleted);

        // 3. Delete file and test deletion tracking
        File.Delete(file3);

        var inc2Result = await engine.CreateBackupAsync(config);
        Assert.True(inc2Result.Success);
        Assert.Equal(2, inc2Result.FilesScanned);
        Assert.Equal(0, inc2Result.FilesAdded);
        Assert.Equal(0, inc2Result.FilesModified);
        Assert.Equal(1, inc2Result.FilesDeleted);

        // 4. Verify Catalog
        var catalog = await engine.GetCatalogAsync(_repoDir);
        Assert.Equal(3, catalog.TotalBackups);
        Assert.Equal(1, catalog.FullBackupsCount);
        Assert.Equal(2, catalog.IncrementalBackupsCount);
    }
}
