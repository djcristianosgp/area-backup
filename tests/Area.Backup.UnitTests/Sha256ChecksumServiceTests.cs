using System.Text;
using Area.Backup.Infrastructure.Hashing;
using Xunit;

namespace Area.Backup.UnitTests;

public class Sha256ChecksumServiceTests
{
    [Fact]
    public async Task Should_Compute_Sha256_Hash_Accurately()
    {
        var service = new Sha256ChecksumService();
        var content = "Hello, Area Backup Engine!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var hash = await service.ComputeSha256Async(stream);

        // Expected SHA-256 for UTF8 "Hello, Area Backup Engine!"
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length); // 64 hex characters

        // Check consistency
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var hash2 = await service.ComputeSha256Async(stream2);
        Assert.Equal(hash, hash2);
    }
}
