using System.IO.Compression;
using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;
using Xunit;

namespace Area.Backup.IntegrationTests;

public class IntegrityAndCorruptionIntegrationTests : IDisposable
{
    private readonly string _testBase;
    private readonly string _sourceDir;
    private readonly string _repoDir;

    public IntegrityAndCorruptionIntegrationTests()
    {
        _testBase = Path.Combine(Path.GetTempPath(), $"AtualIntegrityInteg_{Guid.NewGuid():N}");
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
    public async Task Should_Validate_Valid_Backup_And_Detect_Bit_Flip_Tampering()
    {
        var engine = new BackupEngine();
        var file1 = Path.Combine(_sourceDir, "secure_data.xml");
        await File.WriteAllTextAsync(file1, "<data><record id='1' val='authentic' /></data>");

        var config = new BackupConfiguration
        {
            RepositoryPath = _repoDir,
            BackupType = BackupType.Full,
            Sources = [new BackupSource(_sourceDir, "SEC_SRC")]
        };

        var backupResult = await engine.CreateBackupAsync(config);

        // 1. Validate authentic backup
        var validationBefore = await engine.ValidateBackupAsync(backupResult.BackupPath!, new ValidationOptions
        {
            Mode = ValidationMode.Full
        });

        Assert.True(validationBefore.IsValid);
        Assert.Equal(0, validationBefore.InvalidChecksums);
        Assert.Equal(0, validationBefore.InvalidFiles);

        // 2. Simulate bit-flip corruption inside archive
        var corruptedBackupPath = Path.Combine(_repoDir, "corrupted_test.backup");
        File.Copy(backupResult.BackupPath!, corruptedBackupPath, true);

        // Mutate raw bytes in the corrupted backup file (corrupting the central directory / end of file)
        using (var stream = new FileStream(corruptedBackupPath, FileMode.Open, FileAccess.ReadWrite))
        {
            if (stream.Length > 20)
            {
                stream.Seek(-20, SeekOrigin.End);
                stream.WriteByte(0x00);
                stream.WriteByte(0x00);
                stream.WriteByte(0x00);
            }
        }

        // 3. Validate corrupted backup
        var validationAfter = await engine.ValidateBackupAsync(corruptedBackupPath, new ValidationOptions
        {
            Mode = ValidationMode.Full
        });

        Assert.False(validationAfter.IsValid);
    }
}
