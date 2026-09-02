using System.Diagnostics;
using System.IO.Compression;
using Area.Backup.Core.Enums;
using Area.Backup.Core.Interfaces;
using Area.Backup.Core.Models;

namespace Area.Backup.Infrastructure.Validation;

/// <summary>
/// Validates backup package structure, manifest integrity, and cryptographic payload checksums.
/// </summary>
public sealed class IntegrityValidator : IIntegrityValidator
{
    private readonly IManifestService _manifestService;
    private readonly IChecksumService _checksumService;

    public IntegrityValidator(IManifestService? manifestService = null, IChecksumService? checksumService = null)
    {
        _manifestService = manifestService ?? new Manifest.JsonManifestService();
        _checksumService = checksumService ?? new Hashing.Sha256ChecksumService();
    }

    public async Task<ValidationResult> ValidateAsync(
        string backupPath,
        ValidationOptions options,
        ICatalogRepository? catalog = null,
        IBackupStorage? storage = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        var stopwatch = Stopwatch.StartNew();

        if (!File.Exists(backupPath))
        {
            return new ValidationResult
            {
                IsValid = false,
                BackupPath = backupPath,
                Mode = options.Mode,
                ValidationErrors = { $"Backup file does not exist: {backupPath}" },
                Duration = stopwatch.Elapsed
            };
        }

        var errors = new List<string>();
        var warnings = new List<string>();
        long expectedFiles = 0;
        long validFiles = 0;
        long invalidFiles = 0;
        long missingFiles = 0;
        long invalidChecksums = 0;
        string backupId = string.Empty;
        bool chainValid = true;

        try
        {
            // 1. Read and validate manifest
            var manifest = await _manifestService.ReadManifestFromArchiveAsync(backupPath, cancellationToken);
            backupId = manifest.BackupId;
            expectedFiles = manifest.Files.Count;

            using var zip = ZipFile.OpenRead(backupPath);
            var zipEntries = zip.Entries.ToDictionary(e => e.FullName.Replace('\\', '/').TrimStart('/'), StringComparer.OrdinalIgnoreCase);

            // 2. Validate all files declared in manifest
            foreach (var file in manifest.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (file.ChangeType == FileChangeType.None)
                {
                    // Inherited file from prior backup in the incremental chain
                    validFiles++;
                    continue;
                }

                var entryKey = file.ArchiveEntryPath.Replace('\\', '/').TrimStart('/');
                if (!zipEntries.TryGetValue(entryKey, out var zipEntry))
                {
                    missingFiles++;
                    invalidFiles++;
                    errors.Add($"File '{file.RelativePath}' declared in manifest but missing from archive entries.");
                    continue;
                }

                if (options.Mode == ValidationMode.Full)
                {
                    // Compute SHA-256 directly from entry stream
                    using var stream = zipEntry.Open();
                    var computedHash = await _checksumService.ComputeSha256Async(stream, cancellationToken: cancellationToken);

                    if (!string.Equals(computedHash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        invalidChecksums++;
                        invalidFiles++;
                        errors.Add($"SHA-256 checksum mismatch for '{file.RelativePath}'. Expected: {file.Sha256}, Actual: {computedHash}");
                    }
                    else
                    {
                        validFiles++;
                    }
                }
                else
                {
                    validFiles++;
                }
            }

            // 3. Validate Dependency Chain if incremental
            if (manifest.Type == BackupType.Incremental && !string.IsNullOrEmpty(manifest.ParentBackupId) && catalog != null)
            {
                var parentEntry = await catalog.GetBackupByIdAsync(manifest.ParentBackupId, cancellationToken);
                if (parentEntry == null)
                {
                    chainValid = false;
                    warnings.Add($"Parent backup '{manifest.ParentBackupId}' is not registered in catalog.");
                }
                else if (storage != null)
                {
                    var parentPath = Path.Combine(storage.RootPath, parentEntry.RelativeFilePath);
                    if (!storage.FileExists(parentPath))
                    {
                        chainValid = false;
                        errors.Add($"Parent backup file does not exist on disk: {parentPath}");
                    }
                }
            }

            // 4. Test Restore Simulation if requested
            bool restoreTested = false;
            if (options.PerformTestRestore && errors.Count == 0)
            {
                var tempRestoreDir = Path.Combine(Path.GetTempPath(), $"AtualRestoreTest_{Guid.NewGuid():N}");
                try
                {
                    Directory.CreateDirectory(tempRestoreDir);
                    zip.ExtractToDirectory(tempRestoreDir, overwriteFiles: true);
                    restoreTested = true;
                }
                catch (Exception ex)
                {
                    errors.Add($"Test restore simulation failed: {ex.Message}");
                }
                finally
                {
                    if (Directory.Exists(tempRestoreDir))
                    {
                        try { Directory.Delete(tempRestoreDir, recursive: true); } catch { }
                    }
                }
            }

            stopwatch.Stop();
            bool isValid = errors.Count == 0 && (options.Mode != ValidationMode.Full || invalidChecksums == 0);

            return new ValidationResult
            {
                IsValid = isValid,
                BackupPath = backupPath,
                BackupId = backupId,
                Mode = options.Mode,
                ExpectedFiles = expectedFiles,
                ValidFiles = validFiles,
                InvalidFiles = invalidFiles,
                MissingFiles = missingFiles,
                InvalidChecksums = invalidChecksums,
                RestoreTested = restoreTested,
                DependencyChainValid = chainValid,
                Duration = stopwatch.Elapsed,
                ValidationErrors = errors,
                ValidationWarnings = warnings
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ValidationResult
            {
                IsValid = false,
                BackupPath = backupPath,
                BackupId = backupId,
                Mode = options.Mode,
                ValidationErrors = { $"Integrity validation fatal error: {ex.Message}" },
                Duration = stopwatch.Elapsed
            };
        }
    }
}
