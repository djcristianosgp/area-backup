using Atual.Backup.Core.Interfaces;
using Atual.Backup.Core.Models;
using Atual.Backup.Infrastructure.Scanning;
using Xunit;

namespace Atual.Backup.UnitTests;

public class FileSystemScannerTests : IDisposable
{
    private readonly string _testDir;

    public FileSystemScannerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"ScannerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    public async Task Should_Scan_Directory_And_Apply_Exclusions()
    {
        // Setup folder layout
        File.WriteAllText(Path.Combine(_testDir, "file1.txt"), "hello");
        File.WriteAllText(Path.Combine(_testDir, "file2.tmp"), "temp");

        var subDir = Path.Combine(_testDir, "TempDir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "file3.txt"), "inside temp");

        var validSub = Path.Combine(_testDir, "ValidSub");
        Directory.CreateDirectory(validSub);
        File.WriteAllText(Path.Combine(validSub, "file4.dat"), "valid payload");

        var scanner = new FileSystemScanner();
        var sources = new[] { new BackupSource(_testDir, "SRC_TEST") };
        var exclusions = new[] { new BackupExclusion("*.tmp"), new BackupExclusion("TempDir") };

        var scanned = new List<ScannedFile>();
        await foreach (var file in scanner.ScanSourcesAsync(sources, exclusions, new PerformanceOptions()))
        {
            scanned.Add(file);
        }

        Assert.Equal(2, scanned.Count);
        Assert.Contains(scanned, f => f.RelativePath == "file1.txt");
        Assert.Contains(scanned, f => f.RelativePath == Path.Combine("ValidSub", "file4.dat"));
        Assert.DoesNotContain(scanned, f => f.RelativePath.EndsWith(".tmp"));
        Assert.DoesNotContain(scanned, f => f.RelativePath.Contains("TempDir"));
    }
}
