using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Models;
using Xunit;

namespace Atual.Backup.IntegrationTests;

public class MultipleSourcesIntegrationTests : IDisposable
{
    private readonly string _testBase;
    private readonly string _src1;
    private readonly string _src2;
    private readonly string _src3;
    private readonly string _repoDir;
    private readonly string _restoreDir;

    public MultipleSourcesIntegrationTests()
    {
        _testBase = Path.Combine(Path.GetTempPath(), $"MultiSrcTest_{Guid.NewGuid():N}");
        _src1 = Path.Combine(_testBase, "ERP");
        _src2 = Path.Combine(_testBase, "Documentos");
        _src3 = Path.Combine(_testBase, "XML");
        _repoDir = Path.Combine(_testBase, "Repo");
        _restoreDir = Path.Combine(_testBase, "Restore");

        Directory.CreateDirectory(_src1);
        Directory.CreateDirectory(_src2);
        Directory.CreateDirectory(_src3);
        Directory.CreateDirectory(_repoDir);
        Directory.CreateDirectory(_restoreDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testBase, true); } catch { }
    }

    [Fact]
    public async Task Should_Backup_And_Restore_Multiple_Distinct_Sources_Accurately()
    {
        await File.WriteAllTextAsync(Path.Combine(_src1, "erp.exe"), "ERP Executable Binary Content");
        await File.WriteAllTextAsync(Path.Combine(_src2, "contrato.pdf"), "Contrato Social PDF Content");
        await File.WriteAllTextAsync(Path.Combine(_src3, "nfe_1001.xml"), "<nfe>1001</nfe>");

        var config = new BackupConfiguration
        {
            RepositoryPath = _repoDir,
            BackupType = BackupType.Full,
            Sources =
            [
                new BackupSource(_src1, "SRC_ERP"),
                new BackupSource(_src2, "SRC_DOCS"),
                new BackupSource(_src3, "SRC_XML")
            ]
        };

        var engine = new BackupEngine();
        var result = await engine.CreateBackupAsync(config);

        Assert.True(result.Success);
        Assert.Equal(3, result.FilesScanned);
        Assert.Equal(3, result.FilesAdded);

        // Restore all sources
        var restoreResult = await engine.RestoreBackupAsync(result.BackupPath!, new RestoreOptions
        {
            DestinationPath = _restoreDir
        });

        Assert.True(restoreResult.Success);
        Assert.Equal(3, restoreResult.FilesRestored);

        Assert.True(File.Exists(Path.Combine(_restoreDir, "SRC_ERP", "erp.exe")));
        Assert.True(File.Exists(Path.Combine(_restoreDir, "SRC_DOCS", "contrato.pdf")));
        Assert.True(File.Exists(Path.Combine(_restoreDir, "SRC_XML", "nfe_1001.xml")));
    }
}
