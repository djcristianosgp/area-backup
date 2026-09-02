using System.Diagnostics;
using System.Text.Json;
using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Exceptions;
using Atual.Backup.Core.Interfaces;
using Atual.Backup.Core.Models;
using Atual.Backup.Core.Models.Manifest;
using Atual.Backup.Database.Providers;
using Atual.Backup.Infrastructure.Catalog;
using Atual.Backup.Infrastructure.ChangeDetection;
using Atual.Backup.Infrastructure.Compression;
using Atual.Backup.Infrastructure.Hashing;
using Atual.Backup.Infrastructure.Manifest;
using Atual.Backup.Infrastructure.Restore;
using Atual.Backup.Infrastructure.Retention;
using Atual.Backup.Infrastructure.Scanning;
using Atual.Backup.Infrastructure.Storage;
using Atual.Backup.Infrastructure.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Atual.Backup;

/// <summary>
/// Main engine facade orchestrating Full and Incremental backups, integrity validation,
/// point-in-time restorations, and retention management.
/// </summary>
public sealed class BackupEngine : IBackupEngine
{
    private readonly IFileScanner _scanner;
    private readonly IChangeDetector _changeDetector;
    private readonly IChecksumService _checksumService;
    private readonly ICompressionProvider _compressionProvider;
    private readonly IManifestService _manifestService;
    private readonly IIntegrityValidator _validator;
    private readonly IRestoreEngine _restoreEngine;
    private readonly IRetentionPolicyService _retentionService;
    private readonly DatabaseProviderFactory _databaseFactory;
    private readonly ILogger<BackupEngine> _logger;

    private BackupStatus _status = BackupStatus.Idle;

    public BackupStatus Status => _status;

    public event EventHandler<BackupProgress>? ProgressChanged;
    public event EventHandler<BackupStage>? StageChanged;
    public event EventHandler<BackupResult>? Completed;
    public event EventHandler<BackupError>? Error;

    public BackupEngine(
        IFileScanner? scanner = null,
        IChangeDetector? changeDetector = null,
        IChecksumService? checksumService = null,
        ICompressionProvider? compressionProvider = null,
        IManifestService? manifestService = null,
        IIntegrityValidator? validator = null,
        IRestoreEngine? restoreEngine = null,
        IRetentionPolicyService? retentionService = null,
        DatabaseProviderFactory? databaseFactory = null,
        ILogger<BackupEngine>? logger = null)
    {
        _scanner = scanner ?? new FileSystemScanner();
        _checksumService = checksumService ?? new Sha256ChecksumService();
        _changeDetector = changeDetector ?? new FileSystemChangeDetector(_checksumService);
        _compressionProvider = compressionProvider ?? new ZipCompressionProvider();
        _manifestService = manifestService ?? new JsonManifestService();
        _validator = validator ?? new IntegrityValidator(_manifestService, _checksumService);
        _restoreEngine = restoreEngine ?? new RestoreEngine(_manifestService, _checksumService);
        _retentionService = retentionService ?? new RetentionPolicyService();
        _databaseFactory = databaseFactory ?? new DatabaseProviderFactory();
        _logger = logger ?? NullLogger<BackupEngine>.Instance;
    }

    public async Task<BackupResult> CreateBackupAsync(
        BackupConfiguration configuration,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        var stopwatch = Stopwatch.StartNew();
        var storage = new LocalFileSystemStorage(configuration.RepositoryPath);
        using var catalog = new SqliteCatalogRepository(storage.GetCatalogPath());
        await catalog.InitializeAsync(cancellationToken);

        var baseBackupId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupId = baseBackupId;
        int idCounter = 1;
        while (await catalog.GetBackupByIdAsync(backupId, cancellationToken) != null)
        {
            backupId = $"{baseBackupId}-{idCounter:D2}";
            idCounter++;
        }

        // 1. Acquire repository lock to prevent concurrent runs
        using var repositoryLock = RepositoryLock.Acquire(storage.RootPath);
        storage.CleanupTempFiles();

        _status = BackupStatus.Running;
        string? tempFilePath = null;
        string? finalFilePath = null;
        var warnings = new List<BackupWarning>();
        var errors = new List<BackupError>();

        try
        {
            EmitStage(BackupStage.Initializing);
            _logger.LogInformation("Starting backup process {BackupId} for repository {RepositoryPath}", backupId, storage.RootPath);

            // 2. Resolve Backup Type (Auto vs Full vs Incremental)
            var actualType = configuration.BackupType;
            BackupCatalogEntry? parentBackup = null;
            BackupCatalogEntry? rootFullBackup = null;

            if (actualType == BackupType.Auto)
            {
                actualType = await ResolveAutoBackupTypeAsync(catalog, configuration.Incremental, cancellationToken);
            }

            if (actualType == BackupType.Incremental)
            {
                parentBackup = await catalog.GetLatestBackupAsync(cancellationToken);
                if (parentBackup == null)
                {
                    _logger.LogInformation("No existing backup found in catalog. Upgrading Incremental to Full.");
                    actualType = BackupType.Full;
                }
                else
                {
                    rootFullBackup = parentBackup.Type == BackupType.Full
                        ? parentBackup
                        : await catalog.GetBackupByIdAsync(parentBackup.RootFullBackupId ?? parentBackup.BackupId, cancellationToken);
                }
            }

            // 3. Scan Sources directly
            EmitStage(BackupStage.Scanning);
            var scannedFiles = new List<ScannedFile>();
            await foreach (var file in _scanner.ScanSourcesAsync(configuration.Sources, configuration.Exclusions, configuration.Performance, cancellationToken))
            {
                scannedFiles.Add(file);
                ReportProgress(progress, BackupProgress.Create(
                    BackupStage.Scanning,
                    scannedFiles.Count,
                    0,
                    scannedFiles.Sum(f => f.Size),
                    0,
                    file.FullPath,
                    stopwatch.Elapsed));
            }

            _logger.LogInformation("Scanned {FileCount} files ({Bytes} bytes)", scannedFiles.Count, scannedFiles.Sum(f => f.Size));

            // 4. Change Detection
            EmitStage(BackupStage.DetectingChanges);
            BackupManifest? previousManifest = null;

            if (actualType == BackupType.Incremental && parentBackup != null)
            {
                var parentPath = Path.Combine(storage.RootPath, parentBackup.RelativeFilePath);
                if (File.Exists(parentPath))
                {
                    previousManifest = await _manifestService.ReadManifestFromArchiveAsync(parentPath, cancellationToken);
                }
            }

            var changeResult = await _changeDetector.DetectChangesAsync(
                scannedFiles,
                configuration.Sources,
                previousManifest,
                configuration.Incremental,
                cancellationToken);

            _logger.LogInformation("Change detection finished: Added={Added}, Modified={Modified}, Deleted={Deleted}, Unchanged={Unchanged}",
                changeResult.FilesAdded, changeResult.FilesModified, changeResult.FilesDeleted, changeResult.FilesUnchanged);

            // 5. Database Provider (if enabled)
            DatabaseBackupResult? dbResult = null;
            var dbProvider = _databaseFactory.Resolve(configuration.Database);
            var tempDir = Path.Combine(Path.GetTempPath(), $"AtualBackupTemp_{backupId}");

            if (dbProvider != null)
            {
                Directory.CreateDirectory(tempDir);
                dbResult = await dbProvider.BackupAsync(configuration.Database, tempDir, cancellationToken);
            }

            // 6. Stage & Compress package directly into .tmp file
            finalFilePath = storage.GenerateBackupFilePath(backupId, actualType, DateTime.UtcNow);
            tempFilePath = storage.GetTempFilePath(finalFilePath);

            EmitStage(BackupStage.Reading);
            var manifestFiles = new List<ManifestFileEntry>();
            var archiveEntries = new List<ArchiveEntrySource>();

            long totalBytesToBackup = changeResult.TotalBytesToBackup + (dbResult?.SizeBytes ?? 0);
            long processedBytes = 0;
            long processedFiles = 0;

            foreach (var candidate in changeResult.ChangedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string sha256;
                try
                {
                    sha256 = await _checksumService.ComputeSha256Async(candidate.ScannedFile.FullPath, configuration.Performance.BufferSize, cancellationToken);
                }
                catch (Exception ex)
                {
                    if (configuration.Performance.FailOnLockedFile)
                        throw;

                    warnings.Add(new BackupWarning("FILE_LOCKED", $"Skipping locked file: {ex.Message}", candidate.ScannedFile.FullPath));
                    continue;
                }

                var archiveEntryPath = $"files/{candidate.ScannedFile.SourceId}/{candidate.ScannedFile.RelativePath.Replace('\\', '/')}";

                manifestFiles.Add(new ManifestFileEntry
                {
                    SourceId = candidate.ScannedFile.SourceId,
                    RelativePath = candidate.ScannedFile.RelativePath,
                    Size = candidate.ScannedFile.Size,
                    LastWriteTimeUtc = candidate.ScannedFile.LastWriteTimeUtc,
                    Sha256 = sha256,
                    ArchiveEntryPath = archiveEntryPath,
                    ChangeType = candidate.ChangeType
                });

                archiveEntries.Add(new ArchiveEntrySource
                {
                    EntryPathInArchive = archiveEntryPath,
                    SourceFilePath = candidate.ScannedFile.FullPath,
                    LastWriteTimeUtc = candidate.ScannedFile.LastWriteTimeUtc,
                    Size = candidate.ScannedFile.Size
                });

                processedBytes += candidate.ScannedFile.Size;
                processedFiles++;

                ReportProgress(progress, BackupProgress.Create(
                    BackupStage.Reading,
                    processedFiles,
                    changeResult.ChangedFiles.Count,
                    processedBytes,
                    totalBytesToBackup,
                    candidate.ScannedFile.FullPath,
                    stopwatch.Elapsed));
            }

            // Carry forward unchanged files from previous manifest into the point-in-time snapshot
            if (actualType == BackupType.Incremental && previousManifest != null)
            {
                var changedKeys = changeResult.ChangedFiles
                    .Select(c => $"{c.ScannedFile.SourceId}::{c.ScannedFile.RelativePath.Replace('/', '\\').Trim('\\')}")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var deletedKeys = changeResult.DeletedFiles
                    .Select(d => $"{d.SourceId}::{d.RelativePath.Replace('/', '\\').Trim('\\')}")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var prevFile in previousManifest.Files)
                {
                    var key = $"{prevFile.SourceId}::{prevFile.RelativePath.Replace('/', '\\').Trim('\\')}";
                    if (!changedKeys.Contains(key) && !deletedKeys.Contains(key))
                    {
                        manifestFiles.Add(new ManifestFileEntry
                        {
                            SourceId = prevFile.SourceId,
                            RelativePath = prevFile.RelativePath,
                            Size = prevFile.Size,
                            LastWriteTimeUtc = prevFile.LastWriteTimeUtc,
                            Sha256 = prevFile.Sha256,
                            ArchiveEntryPath = prevFile.ArchiveEntryPath,
                            ChangeType = FileChangeType.None
                        });
                    }
                }
            }

            // Add Database dump entry if present
            if (dbResult != null && File.Exists(dbResult.OutputFilePath))
            {
                archiveEntries.Add(new ArchiveEntrySource
                {
                    EntryPathInArchive = dbResult.EntryNameInArchive,
                    SourceFilePath = dbResult.OutputFilePath,
                    LastWriteTimeUtc = DateTime.UtcNow,
                    Size = dbResult.SizeBytes
                });
            }

            // Build and serialize Manifest
            var manifest = new BackupManifest
            {
                FormatVersion = 1,
                EngineVersion = "1.0.0",
                BackupId = backupId,
                Type = actualType,
                CreatedAtUtc = DateTime.UtcNow,
                ParentBackupId = parentBackup?.BackupId,
                RootFullBackupId = rootFullBackup?.BackupId ?? (actualType == BackupType.Full ? backupId : null),
                Sources = configuration.Sources.Select(s => new ManifestSource
                {
                    SourceId = s.Id,
                    OriginalPath = s.Path,
                    Description = s.Description
                }).ToList(),
                Files = manifestFiles,
                DeletedFiles = changeResult.DeletedFiles,
                DatabaseDumpEntryPath = dbResult?.EntryNameInArchive
            };

            var manifestJson = _manifestService.Serialize(manifest);
            var manifestBytes = System.Text.Encoding.UTF8.GetBytes(manifestJson);

            archiveEntries.Add(new ArchiveEntrySource
            {
                EntryPathInArchive = "manifest.json",
                DirectContentBytes = manifestBytes,
                LastWriteTimeUtc = DateTime.UtcNow,
                Size = manifestBytes.Length
            });

            // Write archive
            EmitStage(BackupStage.Compressing);
            await _compressionProvider.CreateArchiveAsync(tempFilePath, archiveEntries, configuration.Compression, progress, cancellationToken);

            // Clean database temp folder
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }

            // 7. Validate Backup Integrity on .tmp package
            bool integrityValid = true;
            if (configuration.Validation.ValidateAfterBackup)
            {
                EmitStage(BackupStage.Validating);
                var validation = await _validator.ValidateAsync(tempFilePath, configuration.Validation, catalog, storage, cancellationToken);
                if (!validation.IsValid)
                {
                    throw new BackupIntegrityException($"Validation failed for backup '{backupId}': {string.Join("; ", validation.ValidationErrors)}", tempFilePath);
                }
            }

            // 8. Commit Atomic File (.tmp -> .backup)
            EmitStage(BackupStage.Finalizing);
            storage.CommitTempFile(tempFilePath, finalFilePath);

            var finalFileInfo = new FileInfo(finalFilePath);
            var relativeFilePath = Path.GetRelativePath(storage.RootPath, finalFilePath);

            // 9. Register in SQLite Catalog
            var catalogEntry = new BackupCatalogEntry
            {
                BackupId = backupId,
                Type = actualType,
                CreatedAtUtc = DateTime.UtcNow,
                Status = BackupStatus.Completed,
                ParentBackupId = manifest.ParentBackupId,
                RootFullBackupId = manifest.RootFullBackupId,
                RelativeFilePath = relativeFilePath,
                FileCount = manifest.Files.Count,
                DeletedFileCount = manifest.DeletedFiles.Count,
                TotalSizeBytes = manifest.Files.Sum(f => f.Size),
                CompressedSizeBytes = finalFileInfo.Exists ? finalFileInfo.Length : 0,
                EngineVersion = manifest.EngineVersion,
                FormatVersion = manifest.FormatVersion
            };

            await catalog.RegisterBackupAsync(catalogEntry, manifest, cancellationToken);

            // 10. Apply Retention Policy
            if (configuration.Retention.Enabled)
            {
                await _retentionService.ApplyRetentionAsync(catalog, storage, configuration.Retention, cancellationToken);
            }

            stopwatch.Stop();
            _status = BackupStatus.Completed;
            EmitStage(BackupStage.Completed);

            var result = new BackupResult
            {
                Success = true,
                BackupId = backupId,
                Type = actualType,
                ParentBackupId = manifest.ParentBackupId,
                BackupPath = finalFilePath,
                CreatedAtUtc = catalogEntry.CreatedAtUtc,
                FilesScanned = changeResult.TotalFilesScanned,
                FilesAdded = changeResult.FilesAdded,
                FilesModified = changeResult.FilesModified,
                FilesDeleted = changeResult.FilesDeleted,
                FilesSkipped = warnings.Count,
                BytesScanned = changeResult.TotalBytesScanned,
                BytesBackedUp = manifest.Files.Sum(f => f.Size),
                CompressedSize = catalogEntry.CompressedSizeBytes,
                Duration = stopwatch.Elapsed,
                IntegrityValidated = integrityValid,
                Warnings = warnings,
                Errors = errors
            };

            Completed?.Invoke(this, result);
            return result;
        }
        catch (OperationCanceledException)
        {
            _status = BackupStatus.Cancelled;
            if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                try { File.Delete(tempFilePath); } catch { }
            }
            throw;
        }
        catch (Exception ex)
        {
            _status = BackupStatus.Failed;
            if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                try { File.Delete(tempFilePath); } catch { }
            }

            var error = new BackupError("BACKUP_FAILED", ex.Message, finalFilePath, ex);
            errors.Add(error);
            Error?.Invoke(this, error);
            throw;
        }
    }

    public async Task<ValidationResult> ValidateBackupAsync(
        string backupPath,
        ValidationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var opts = options ?? new ValidationOptions { Mode = ValidationMode.Quick };
        _status = BackupStatus.Validating;

        try
        {
            var result = await _validator.ValidateAsync(backupPath, opts, cancellationToken: cancellationToken);
            _status = BackupStatus.Idle;
            return result;
        }
        catch
        {
            _status = BackupStatus.Failed;
            throw;
        }
    }

    public async Task<RestoreResult> RestoreBackupAsync(
        string backupPath,
        RestoreOptions options,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        ArgumentNullException.ThrowIfNull(options);

        var repoDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(backupPath)))
            ?? Path.GetDirectoryName(backupPath)!;

        var storage = new LocalFileSystemStorage(repoDir);
        using var catalog = new SqliteCatalogRepository(storage.GetCatalogPath());
        await catalog.InitializeAsync(cancellationToken);

        _status = BackupStatus.Restoring;

        try
        {
            var result = await _restoreEngine.RestoreAsync(backupPath, options, catalog, storage, progress, cancellationToken);
            _status = BackupStatus.Idle;
            return result;
        }
        catch
        {
            _status = BackupStatus.Failed;
            throw;
        }
    }

    public BackupInfo GetBackupInfo(string backupPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);

        if (!File.Exists(backupPath))
            throw new FileNotFoundException($"Backup archive was not found: {backupPath}");

        var fileInfo = new FileInfo(backupPath);
        var manifest = _manifestService.ReadManifestFromArchiveAsync(backupPath).GetAwaiter().GetResult();

        return new BackupInfo
        {
            BackupId = manifest.BackupId,
            CreatedAtUtc = manifest.CreatedAtUtc,
            Type = manifest.Type,
            ParentBackupId = manifest.ParentBackupId,
            RootFullBackupId = manifest.RootFullBackupId,
            FileCount = manifest.Files.Count,
            DeletedFileCount = manifest.DeletedFiles.Count,
            TotalSizeBytes = manifest.Files.Sum(f => f.Size),
            CompressedSizeBytes = fileInfo.Length,
            BackupPath = backupPath,
            EngineVersion = manifest.EngineVersion,
            FormatVersion = manifest.FormatVersion,
            IsDatabaseIncluded = !string.IsNullOrEmpty(manifest.DatabaseDumpEntryPath),
            Sources = manifest.Sources.Select(s => s.OriginalPath).ToList()
        };
    }

    public async Task<BackupCatalog> GetCatalogAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);
        var storage = new LocalFileSystemStorage(repositoryPath);
        using var catalogRepo = new SqliteCatalogRepository(storage.GetCatalogPath());
        await catalogRepo.InitializeAsync(cancellationToken);

        var entries = await catalogRepo.GetAllBackupsAsync(cancellationToken);
        return new BackupCatalog
        {
            RepositoryPath = repositoryPath,
            Entries = entries.ToList()
        };
    }

    private static async Task<BackupType> ResolveAutoBackupTypeAsync(
        ICatalogRepository catalog,
        IncrementalOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.Enabled) return BackupType.Full;

        var latestFull = await catalog.GetLatestFullBackupAsync(cancellationToken);
        if (latestFull == null) return BackupType.Full;

        // Check age since last full
        if (options.MaxDaysSinceFull > 0)
        {
            var age = DateTime.UtcNow - latestFull.CreatedAtUtc;
            if (age.TotalDays >= options.MaxDaysSinceFull)
                return BackupType.Full;
        }

        // Check count of incrementals since last full
        if (options.MaxIncrementalBackups > 0)
        {
            var count = await catalog.GetIncrementalCountSinceLastFullAsync(cancellationToken);
            if (count >= options.MaxIncrementalBackups)
                return BackupType.Full;
        }

        return BackupType.Incremental;
    }

    private void EmitStage(BackupStage stage) => StageChanged?.Invoke(this, stage);

    private void ReportProgress(IProgress<BackupProgress>? progress, BackupProgress state)
    {
        progress?.Report(state);
        ProgressChanged?.Invoke(this, state);
    }
}
