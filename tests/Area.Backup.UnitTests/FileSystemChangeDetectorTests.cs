using Area.Backup.Core.Enums;
using Area.Backup.Core.Interfaces;
using Area.Backup.Core.Models;
using Area.Backup.Core.Models.Manifest;
using Area.Backup.Infrastructure.ChangeDetection;
using Xunit;

namespace Area.Backup.UnitTests;

public class FileSystemChangeDetectorTests
{
    [Fact]
    public async Task Should_Classify_All_Files_As_Added_When_No_Previous_Manifest()
    {
        var detector = new FileSystemChangeDetector();
        var files = new List<ScannedFile>
        {
            new("SRC_1", @"C:\ERP\file1.txt", "file1.txt", 100, DateTime.UtcNow),
            new("SRC_1", @"C:\ERP\file2.txt", "file2.txt", 200, DateTime.UtcNow)
        };
        var sources = new[] { new BackupSource(@"C:\ERP", "SRC_1") };

        var result = await detector.DetectChangesAsync(files, sources, previousManifest: null, new IncrementalOptions());

        Assert.Equal(2, result.ChangedFiles.Count);
        Assert.All(result.ChangedFiles, f => Assert.Equal(FileChangeType.Added, f.ChangeType));
        Assert.Empty(result.DeletedFiles);
        Assert.Equal(0, result.FilesUnchanged);
    }

    [Fact]
    public async Task Should_Detect_Added_Modified_Unchanged_And_Deleted_Files()
    {
        var detector = new FileSystemChangeDetector();
        var sources = new[] { new BackupSource(@"C:\ERP", "SRC_1") };

        var baseTime = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        var previousManifest = new BackupManifest
        {
            Files = new List<ManifestFileEntry>
            {
                new() { SourceId = "SRC_1", RelativePath = "unchanged.txt", Size = 100, LastWriteTimeUtc = baseTime, Sha256 = "hash1" },
                new() { SourceId = "SRC_1", RelativePath = "modified.txt", Size = 200, LastWriteTimeUtc = baseTime, Sha256 = "hash2" },
                new() { SourceId = "SRC_1", RelativePath = "deleted.txt", Size = 300, LastWriteTimeUtc = baseTime, Sha256 = "hash3" }
            }
        };

        var currentFiles = new List<ScannedFile>
        {
            new("SRC_1", @"C:\ERP\unchanged.txt", "unchanged.txt", 100, baseTime), // Unchanged
            new("SRC_1", @"C:\ERP\modified.txt", "modified.txt", 250, baseTime.AddHours(1)), // Modified
            new("SRC_1", @"C:\ERP\newfile.txt", "newfile.txt", 500, baseTime.AddHours(2)) // Added
            // deleted.txt is missing from currentFiles -> Deleted
        };

        var result = await detector.DetectChangesAsync(currentFiles, sources, previousManifest, new IncrementalOptions());

        Assert.Equal(2, result.ChangedFiles.Count); // modified + newfile
        Assert.Contains(result.ChangedFiles, f => f.ScannedFile.RelativePath == "newfile.txt" && f.ChangeType == FileChangeType.Added);
        Assert.Contains(result.ChangedFiles, f => f.ScannedFile.RelativePath == "modified.txt" && f.ChangeType == FileChangeType.Modified);
        Assert.Single(result.DeletedFiles);
        Assert.Equal("deleted.txt", result.DeletedFiles[0].RelativePath);
        Assert.Equal(1, result.FilesUnchanged);
    }
}
