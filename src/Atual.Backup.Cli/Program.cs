using System.Diagnostics;
using System.Text.Json;
using Atual.Backup;
using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Models;
using Microsoft.Extensions.Logging;

namespace Atual.Backup.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
        PrintHeader();

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var arguments = ParseArguments(args.Skip(1).ToArray());

        try
        {
            switch (command)
            {
                case "backup":
                    return await ExecuteBackupCommandAsync(arguments);

                case "validate":
                    return await ExecuteValidateCommandAsync(arguments);

                case "restore":
                    return await ExecuteRestoreCommandAsync(arguments);

                case "info":
                    return ExecuteInfoCommand(arguments);

                case "list":
                    return await ExecuteListCommandAsync(arguments);

                case "benchmark":
                    return await ExecuteBenchmarkCommandAsync(arguments);

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[!] Unknown command: {command}");
                    Console.ResetColor();
                    PrintUsage();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR] Command failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Details: {ex.InnerException.Message}");
            }
            Console.ResetColor();
            return 1;
        }
    }

    private static async Task<int> ExecuteBackupCommandAsync(Dictionary<string, string> args)
    {
        if (!args.TryGetValue("config", out var configPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[!] Missing required parameter: --config <path-to-json>");
            Console.ResetColor();
            return 1;
        }

        if (!File.Exists(configPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[!] Configuration file not found: {configPath}");
            Console.ResetColor();
            return 1;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<BackupConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        });

        if (config == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[!] Failed to parse configuration JSON.");
            Console.ResetColor();
            return 1;
        }

        Console.WriteLine($"Loaded configuration from: {configPath}");
        Console.WriteLine($"Repository: {config.RepositoryPath}");
        Console.WriteLine($"Type: {config.BackupType}");
        Console.WriteLine($"Sources ({config.Sources.Count}):");
        foreach (var src in config.Sources)
        {
            Console.WriteLine($"  - [{src.Id}] {src.Path}");
        }
        Console.WriteLine(new string('-', 60));

        var engine = new BackupEngine();
        using var cts = new CancellationTokenSource();
        try
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n[!] Cancellation requested... Safely aborting backup.");
                cts.Cancel();
            };
        }
        catch
        {
            // Ignore if running without console attached
        }

        var progress = new Progress<BackupProgress>(p =>
        {
            var bar = GetProgressBar(p.Percentage, 25);
            var file = string.IsNullOrEmpty(p.CurrentFile) ? "" : Path.GetFileName(p.CurrentFile);
            if (file.Length > 25) file = file[..22] + "...";

            var speedMb = p.SpeedBytesPerSecond / (1024.0 * 1024.0);
            var speedStr = speedMb > 0 ? $"{speedMb:F1} MB/s" : "-- MB/s";

            Console.Write($"\r[{p.Stage,-15}] {bar} {p.Percentage,5:F1}% | Files: {p.FilesProcessed}/{p.FilesTotal} | {speedStr} | {file,-28}");
        });

        var result = await engine.CreateBackupAsync(config, progress, cts.Token);

        Console.WriteLine("\n" + new string('=', 60));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ BACKUP COMPLETED SUCCESSFULLY");
        Console.ResetColor();
        Console.WriteLine($"Backup ID:       {result.BackupId}");
        Console.WriteLine($"Type:            {result.Type}");
        Console.WriteLine($"Archive:         {result.BackupPath}");
        Console.WriteLine($"Files Scanned:   {result.FilesScanned:N0}");
        Console.WriteLine($"Files Added:     {result.FilesAdded:N0}");
        Console.WriteLine($"Files Modified:  {result.FilesModified:N0}");
        Console.WriteLine($"Files Deleted:   {result.FilesDeleted:N0}");
        Console.WriteLine($"Bytes Backed Up: {FormatBytes(result.BytesBackedUp)}");
        Console.WriteLine($"Compressed Size: {FormatBytes(result.CompressedSize)}");
        Console.WriteLine($"Duration:        {result.Duration.TotalSeconds:F2}s");
        Console.WriteLine($"Integrity Check: {(result.IntegrityValidated ? "✓ Passed (SHA-256)" : "Skipped")}");

        if (result.Warnings.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nWarnings ({result.Warnings.Count}):");
            foreach (var w in result.Warnings)
            {
                Console.WriteLine($"  [!] {w.Code}: {w.Message} ({w.Path})");
            }
            Console.ResetColor();
        }

        return 0;
    }

    private static async Task<int> ExecuteValidateCommandAsync(Dictionary<string, string> args)
    {
        if (!args.TryGetValue("backup", out var backupPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[!] Missing required parameter: --backup <path-to-backup-file>");
            Console.ResetColor();
            return 1;
        }

        var mode = ValidationMode.Full;
        if (args.TryGetValue("mode", out var modeStr) && Enum.TryParse<ValidationMode>(modeStr, true, out var parsedMode))
        {
            mode = parsedMode;
        }

        Console.WriteLine($"Validating backup: {backupPath}");
        Console.WriteLine($"Mode: {mode}");

        var engine = new BackupEngine();
        var validation = await engine.ValidateBackupAsync(backupPath, new ValidationOptions
        {
            Mode = mode,
            PerformTestRestore = args.ContainsKey("test-restore")
        });

        Console.WriteLine(new string('-', 60));
        if (validation.IsValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ BACKUP INTEGRITY IS VALID");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ BACKUP INTEGRITY CHECK FAILED");
            Console.ResetColor();
        }

        Console.WriteLine($"Backup ID:          {validation.BackupId}");
        Console.WriteLine($"Expected Files:     {validation.ExpectedFiles:N0}");
        Console.WriteLine($"Valid Files:        {validation.ValidFiles:N0}");
        Console.WriteLine($"Invalid Files:      {validation.InvalidFiles:N0}");
        Console.WriteLine($"Missing Files:      {validation.MissingFiles:N0}");
        Console.WriteLine($"Invalid Checksums:  {validation.InvalidChecksums:N0}");
        Console.WriteLine($"Chain Valid:        {(validation.DependencyChainValid ? "Yes" : "No")}");
        Console.WriteLine($"Restore Tested:     {(validation.RestoreTested ? "Yes" : "No")}");
        Console.WriteLine($"Duration:           {validation.Duration.TotalSeconds:F2}s");

        if (validation.ValidationErrors.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nErrors:");
            foreach (var err in validation.ValidationErrors)
            {
                Console.WriteLine($"  [-] {err}");
            }
            Console.ResetColor();
        }

        return validation.IsValid ? 0 : 1;
    }

    private static async Task<int> ExecuteRestoreCommandAsync(Dictionary<string, string> args)
    {
        if (!args.TryGetValue("backup", out var backupPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[!] Missing required parameter: --backup <path-to-backup-file>");
            Console.ResetColor();
            return 1;
        }

        if (!args.TryGetValue("destination", out var destinationPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[!] Missing required parameter: --destination <output-dir>");
            Console.ResetColor();
            return 1;
        }

        var options = new RestoreOptions
        {
            DestinationPath = destinationPath,
            SourceId = args.GetValueOrDefault("source"),
            RelativePathFilter = args.GetValueOrDefault("filter"),
            OverwriteExisting = !args.TryGetValue("overwrite", out var ow) || bool.Parse(ow),
            VerifyChecksumsAfterRestore = true,
            ApplyDeletions = true
        };

        Console.WriteLine($"Restoring from: {backupPath}");
        Console.WriteLine($"Destination:    {destinationPath}");
        if (!string.IsNullOrEmpty(options.SourceId)) Console.WriteLine($"Source Filter:  {options.SourceId}");

        var engine = new BackupEngine();
        var progress = new Progress<BackupProgress>(p =>
        {
            Console.Write($"\rRestoring files: {p.FilesProcessed:N0} ({FormatBytes(p.BytesProcessed)}) - {p.CurrentFile}");
        });

        var result = await engine.RestoreBackupAsync(backupPath, options, progress);

        Console.WriteLine("\n" + new string('=', 60));
        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ RESTORE COMPLETED SUCCESSFULLY");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ RESTORE COMPLETED WITH ERRORS");
            Console.ResetColor();
        }

        Console.WriteLine($"Target Backup ID:  {result.TargetBackupId}");
        Console.WriteLine($"Backups in Chain:  {result.BackupsInChainCount}");
        Console.WriteLine($"Files Restored:    {result.FilesRestored:N0}");
        Console.WriteLine($"Files Overwritten: {result.FilesOverwritten:N0}");
        Console.WriteLine($"Files Deleted:     {result.FilesDeleted:N0}");
        Console.WriteLine($"Bytes Restored:    {FormatBytes(result.BytesRestored)}");
        Console.WriteLine($"Duration:          {result.Duration.TotalSeconds:F2}s");

        return result.Success ? 0 : 1;
    }

    private static int ExecuteInfoCommand(Dictionary<string, string> args)
    {
        if (!args.TryGetValue("backup", out var backupPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[!] Missing required parameter: --backup <path-to-backup-file>");
            Console.ResetColor();
            return 1;
        }

        var engine = new BackupEngine();
        var info = engine.GetBackupInfo(backupPath);

        Console.WriteLine($"Backup Information: {Path.GetFileName(backupPath)}");
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"Backup ID:         {info.BackupId}");
        Console.WriteLine($"Type:              {info.Type}");
        Console.WriteLine($"Created At (UTC):  {info.CreatedAtUtc:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Parent Backup ID:  {info.ParentBackupId ?? "--"}");
        Console.WriteLine($"Root Full ID:      {info.RootFullBackupId ?? "--"}");
        Console.WriteLine($"Files:             {info.FileCount:N0}");
        Console.WriteLine($"Deleted Files:     {info.DeletedFileCount:N0}");
        Console.WriteLine($"Uncompressed Size: {FormatBytes(info.TotalSizeBytes)}");
        Console.WriteLine($"Compressed Size:   {FormatBytes(info.CompressedSizeBytes)}");
        Console.WriteLine($"Database Included: {(info.IsDatabaseIncluded ? "Yes" : "No")}");
        Console.WriteLine($"Engine Version:    {info.EngineVersion} (Format: v{info.FormatVersion})");
        Console.WriteLine($"Sources ({info.Sources.Count}):");
        foreach (var s in info.Sources)
        {
            Console.WriteLine($"  - {s}");
        }

        return 0;
    }

    private static async Task<int> ExecuteListCommandAsync(Dictionary<string, string> args)
    {
        if (!args.TryGetValue("repository", out var repoPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[!] Missing required parameter: --repository <path-to-repo>");
            Console.ResetColor();
            return 1;
        }

        var engine = new BackupEngine();
        var catalog = await engine.GetCatalogAsync(repoPath);

        Console.WriteLine($"Catalog for repository: {repoPath}");
        Console.WriteLine($"Total Backups: {catalog.TotalBackups} ({catalog.FullBackupsCount} Full, {catalog.IncrementalBackupsCount} Incremental)");
        Console.WriteLine($"Total Size:    {FormatBytes(catalog.TotalSizeBytes)}");
        Console.WriteLine(new string('-', 85));
        Console.WriteLine($"{"BACKUP ID",-18} | {"TYPE",-12} | {"DATE (UTC)",-19} | {"FILES",8} | {"SIZE",10} | {"STATUS",-10}");
        Console.WriteLine(new string('-', 85));

        foreach (var e in catalog.Entries)
        {
            Console.WriteLine($"{e.BackupId,-18} | {e.Type,-12} | {e.CreatedAtUtc:yyyy-MM-dd HH:mm:ss} | {e.FileCount,8:N0} | {FormatBytes(e.CompressedSizeBytes),10} | {e.Status,-10}");
        }

        return 0;
    }

    private static async Task<int> ExecuteBenchmarkCommandAsync(Dictionary<string, string> args)
    {
        int fileCount = args.TryGetValue("files", out var f) ? int.Parse(f) : 1000;
        int fileSizeKb = args.TryGetValue("size", out var s) ? int.Parse(s) : 16;

        Console.WriteLine($"=== ATUAL BACKUP ENGINE BENCHMARK ===");
        Console.WriteLine($"Generating {fileCount:N0} synthetic files ({fileSizeKb} KB each)...");

        var testBase = Path.Combine(Path.GetTempPath(), $"AtualBenchmark_{Guid.NewGuid():N}");
        var sourceDir = Path.Combine(testBase, "Source");
        var repoDir = Path.Combine(testBase, "Repo");
        var restoreDir = Path.Combine(testBase, "Restore");

        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(repoDir);

        var random = new Random(42);
        var buffer = new byte[fileSizeKb * 1024];

        for (int i = 0; i < fileCount; i++)
        {
            random.NextBytes(buffer);
            var subFolder = Path.Combine(sourceDir, $"Folder_{i % 20}");
            Directory.CreateDirectory(subFolder);
            await File.WriteAllBytesAsync(Path.Combine(subFolder, $"file_{i:D6}.dat"), buffer);
        }

        Console.WriteLine($"Generated test dataset: {fileCount} files ({fileCount * fileSizeKb / 1024.0:F2} MB)\n");

        var engine = new BackupEngine();
        var config = new BackupConfiguration
        {
            RepositoryPath = repoDir,
            BackupType = BackupType.Full,
            Sources = [new BackupSource(sourceDir, "BENCH_SRC")],
            Compression = new CompressionOptions { Algorithm = CompressionAlgorithm.Zip, Level = CompressionLevel.Fastest }
        };

        // 1. Full Backup Benchmark
        Console.WriteLine(">>> 1. Running FULL Backup...");
        var sw = Stopwatch.StartNew();
        var fullResult = await engine.CreateBackupAsync(config);
        sw.Stop();
        Console.WriteLine($"FULL Backup completed in {sw.Elapsed.TotalSeconds:F2}s | Size: {FormatBytes(fullResult.CompressedSize)} | Speed: {fullResult.BytesBackedUp / (1024.0 * 1024.0) / sw.Elapsed.TotalSeconds:F2} MB/s");

        // 2. Incremental with 5% changed files
        Console.WriteLine("\n>>> 2. Modifying 5% of files and running INCREMENTAL Backup...");
        int modifyCount = Math.Max(1, fileCount / 20);
        for (int i = 0; i < modifyCount; i++)
        {
            random.NextBytes(buffer);
            var subFolder = Path.Combine(sourceDir, $"Folder_{i % 20}");
            await File.WriteAllBytesAsync(Path.Combine(subFolder, $"file_{i:D6}.dat"), buffer);
        }

        config.BackupType = BackupType.Incremental;
        sw.Restart();
        var incResult = await engine.CreateBackupAsync(config);
        sw.Stop();
        Console.WriteLine($"INCREMENTAL Backup completed in {sw.Elapsed.TotalSeconds:F2}s | Files Processed: {incResult.FilesModified} / {fileCount} | Size: {FormatBytes(incResult.CompressedSize)}");

        // 3. Restore Benchmark
        Console.WriteLine("\n>>> 3. Running RESTORE to clean destination...");
        sw.Restart();
        var restoreResult = await engine.RestoreBackupAsync(incResult.BackupPath!, new RestoreOptions
        {
            DestinationPath = restoreDir,
            OverwriteExisting = true
        });
        sw.Stop();
        Console.WriteLine($"RESTORE completed in {sw.Elapsed.TotalSeconds:F2}s | Restored: {restoreResult.FilesRestored:N0} files | Success: {restoreResult.Success}");

        // Cleanup
        try { Directory.Delete(testBase, true); } catch { }

        Console.WriteLine("\n✓ Benchmark finished successfully.");
        return 0;
    }

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║            ATUAL BACKUP ENGINE — CLI TOOL v1.0               ║");
        Console.WriteLine("║        Enterprise Incremental Backup & Recovery Solution     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
Usage:
  Atual.Backup.Cli backup --config <path-to-json>
  Atual.Backup.Cli validate --backup <path> [--mode Quick|Full] [--test-restore]
  Atual.Backup.Cli restore --backup <path> --destination <dir> [--source <id>] [--filter <prefix>]
  Atual.Backup.Cli info --backup <path>
  Atual.Backup.Cli list --repository <dir>
  Atual.Backup.Cli benchmark [--files <count>] [--size <kb>]

Examples:
  Atual.Backup.Cli backup --config ./config.sample.json
  Atual.Backup.Cli validate --backup C:\Backup\Atual\2026\09\20260902-143000-full.backup --mode Full
  Atual.Backup.Cli restore --backup C:\Backup\Atual\2026\09\20260902-143000-full.backup --destination C:\Restore
  Atual.Backup.Cli benchmark --files 5000 --size 32
""");
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--") || args[i].StartsWith('-'))
            {
                var key = args[i].TrimStart('-');
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    result[key] = args[i + 1];
                    i++;
                }
                else
                {
                    result[key] = "true";
                }
            }
        }

        return result;
    }

    private static string GetProgressBar(double percentage, int width)
    {
        int filled = (int)Math.Round(width * Math.Clamp(percentage, 0, 100) / 100.0);
        int empty = width - filled;
        return $"[{new string('█', filled)}{new string('░', empty)}]";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
