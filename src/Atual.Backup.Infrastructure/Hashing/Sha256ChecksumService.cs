using System.Security.Cryptography;
using Atual.Backup.Core.Interfaces;

namespace Atual.Backup.Infrastructure.Hashing;

/// <summary>
/// Memory-safe streaming SHA-256 checksum calculator.
/// </summary>
public sealed class Sha256ChecksumService : IChecksumService
{
    public async Task<string> ComputeSha256Async(string filePath, int bufferSize = 65536, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await ComputeSha256Async(fileStream, bufferSize, cancellationToken);
    }

    public async Task<string> ComputeSha256Async(Stream stream, int bufferSize = 65536, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var sha256 = SHA256.Create();
        var buffer = new byte[Math.Max(4096, bufferSize)];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        return Convert.ToHexStringLower(sha256.Hash!);
    }
}
