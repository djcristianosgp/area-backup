using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Exceptions;
using Atual.Backup.Core.Models.Manifest;
using Atual.Backup.Infrastructure.Manifest;
using Xunit;

namespace Atual.Backup.UnitTests;

public class JsonManifestServiceTests
{
    [Fact]
    public void Should_Serialize_And_Deserialize_Manifest_Correctly()
    {
        var service = new JsonManifestService();
        var manifest = new BackupManifest
        {
            FormatVersion = 1,
            EngineVersion = "1.0.0",
            BackupId = "20260902-120000",
            Type = BackupType.Incremental,
            CreatedAtUtc = DateTime.UtcNow,
            ParentBackupId = "20260901-120000",
            RootFullBackupId = "20260901-120000",
            Sources = [new ManifestSource { SourceId = "SRC_1", OriginalPath = @"C:\ERP" }],
            Files =
            [
                new ManifestFileEntry
                {
                    SourceId = "SRC_1",
                    RelativePath = "data.db",
                    Size = 1024,
                    LastWriteTimeUtc = DateTime.UtcNow,
                    Sha256 = "abc123456",
                    ArchiveEntryPath = "files/SRC_1/data.db",
                    ChangeType = FileChangeType.Modified
                }
            ],
            DeletedFiles =
            [
                new ManifestDeletedFile
                {
                    SourceId = "SRC_1",
                    RelativePath = "old.txt",
                    DeletedAtUtc = DateTime.UtcNow
                }
            ]
        };

        var json = service.Serialize(manifest);
        Assert.NotNull(json);
        Assert.Contains("20260902-120000", json);

        var deserialized = service.Deserialize(json);
        Assert.Equal(manifest.BackupId, deserialized.BackupId);
        Assert.Equal(manifest.Type, deserialized.Type);
        Assert.Single(deserialized.Files);
        Assert.Single(deserialized.DeletedFiles);
        Assert.Equal("data.db", deserialized.Files[0].RelativePath);
    }

    [Fact]
    public void Should_Reject_Unsupported_Format_Versions()
    {
        var service = new JsonManifestService();
        var futureJson = """
        {
            "formatVersion": 999,
            "engineVersion": "99.0.0",
            "backupId": "future-id",
            "type": "Full",
            "createdAtUtc": "2030-01-01T00:00:00Z"
        }
        """;

        Assert.Throws<BackupIntegrityException>(() => service.Deserialize(futureJson));
    }
}
