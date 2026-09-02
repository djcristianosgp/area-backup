using Area.Backup.Core.Models;
using Area.Backup.Infrastructure.Scanning;
using Xunit;

namespace Area.Backup.UnitTests;

public class ExclusionMatcherTests
{
    [Fact]
    public void Should_Exclude_By_Wildcard_Extension()
    {
        var exclusions = new[] { new BackupExclusion("*.tmp"), new BackupExclusion("*.log") };
        var matcher = new ExclusionMatcher(exclusions);

        Assert.True(matcher.IsFileExcluded(@"C:\ERP\test.tmp", "test.tmp"));
        Assert.True(matcher.IsFileExcluded(@"C:\ERP\app.log", "app.log"));
        Assert.False(matcher.IsFileExcluded(@"C:\ERP\data.dat", "data.dat"));
    }

    [Fact]
    public void Should_Exclude_By_Directory_Name()
    {
        var exclusions = new[] { new BackupExclusion("Temp", ExclusionType.DirectoryName), new BackupExclusion("Cache", ExclusionType.DirectoryName) };
        var matcher = new ExclusionMatcher(exclusions);

        Assert.True(matcher.IsDirectoryExcluded(@"C:\ERP\Temp", "Temp"));
        Assert.True(matcher.IsDirectoryExcluded(@"C:\ERP\Sub\Cache", "Cache"));
        Assert.False(matcher.IsDirectoryExcluded(@"C:\ERP\Data0001", "Data0001"));
    }

    [Fact]
    public void Should_Exclude_By_Exact_Path()
    {
        var targetPath = Path.GetFullPath(@"C:\ERP\SpecialFolder");
        var exclusions = new[] { new BackupExclusion(targetPath, ExclusionType.ExactPath) };
        var matcher = new ExclusionMatcher(exclusions);

        Assert.True(matcher.IsDirectoryExcluded(targetPath, "SpecialFolder"));
        Assert.False(matcher.IsDirectoryExcluded(@"C:\ERP\OtherFolder", "OtherFolder"));
    }
}
