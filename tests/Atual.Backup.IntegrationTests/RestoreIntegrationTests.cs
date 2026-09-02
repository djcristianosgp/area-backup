using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Models;
using Xunit;

namespace Atual.Backup.IntegrationTests;

public class RestoreIntegrationTests : IDisposable
{
    private readonly string _testBase;
    private readonly string _sourceDir;
    private readonly string _repoDir;
    private readonly string _restoreDir;

    public RestoreIntegrationTests()
    {
        _testBase = Path.Combine(Path.GetTempPath(), $"AtualRestoreInteg_{Guid.NewGuid():N}");
        _sourceDir = Path.Combine(_testBase, "Source");
        _repoDir = Path.Combine(_testBase, "Repo");
        _restoreDir = Path.Combine(_testBase, "Restored");

        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_repoDir);
        Directory.CreateDirectory(_restoreDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testBase, true); } catch { }
    }

    [Fact]
    public async Task Should_Reconstruct_State_From_Full_And_Incremental_Chain_Accurately()
    {
        var engine = new BackupEngine();

        // 1. Initial State
        var doc1 = Path.Combine(_sourceDir, "doc1.txt");
        var doc2 = Path.Combine(_sourceDir, "doc2.txt");
        await File.WriteAllTextAsync(doc1, "Original Document 1");
        await File.WriteAllTextAsync(doc2, "Original Document 2");

        var config = new BackupConfiguration
        {
            RepositoryPath = _repoDir,
            BackupType = BackupType.Full,
            Sources = [new BackupSource(_sourceDir, "DOC_SRC")]
        };

        var fullResult = await engine.CreateBackupAsync(config);

        // 2. Incremental State: Modify doc1, delete doc2, add doc3
        await Task.Delay(1100);
        await File.WriteAllTextAsync(doc1, "Modified Document 1 Content - V2");
        File.Delete(doc2);
        var doc3 = Path.Combine(_sourceDir, "doc3.txt");
        await File.WriteAllTextAsync(doc3, "New Document 3");

        config.BackupType = BackupType.Incremental;
        var incResult = await engine.CreateBackupAsync(config);

        // 3. Restore Target Point (incResult) to clean restore directory
        var restoreOptions = new RestoreOptions
        {
            DestinationPath = _restoreDir,
            OverwriteExisting = true,
            ApplyDeletions = true,
            VerifyChecksumsAfterRestore = true
        };

        var restoreResult = await engine.RestoreBackupAsync(incResult.BackupPath!, restoreOptions);

        Assert.True(restoreResult.Success);
        Assert.Equal(2, restoreResult.BackupsInChainCount); // Full + Inc

        var restoredDoc1 = Path.Combine(_restoreDir, "DOC_SRC", "doc1.txt");
        var restoredDoc2 = Path.Combine(_restoreDir, "DOC_SRC", "doc2.txt");
        var restoredDoc3 = Path.Combine(_restoreDir, "DOC_SRC", "doc3.txt");

        // Verify doc1 has V2 content
        Assert.True(File.Exists(restoredDoc1));
        var content1 = await File.ReadAllTextAsync(restoredDoc1);
        Assert.Equal("Modified Document 1 Content - V2", content1);

        // Verify doc2 was deleted
        Assert.False(File.Exists(restoredDoc2));

        // Verify doc3 exists
        Assert.True(File.Exists(restoredDoc3));
        var content3 = await File.ReadAllTextAsync(restoredDoc3);
        Assert.Equal("New Document 3", content3);
    }
}
