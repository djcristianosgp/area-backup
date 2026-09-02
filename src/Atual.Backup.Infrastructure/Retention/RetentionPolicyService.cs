using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Interfaces;
using Atual.Backup.Core.Models;

namespace Atual.Backup.Infrastructure.Retention;

/// <summary>
/// Enforces backup retention policies while strictly protecting Full backups with active dependent incrementals.
/// </summary>
public sealed class RetentionPolicyService : IRetentionPolicyService
{
    public async Task<RetentionExecutionResult> ApplyRetentionAsync(
        ICatalogRepository catalog,
        IBackupStorage storage,
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(policy);

        var result = new RetentionExecutionResult();
        if (!policy.Enabled) return result;

        var allBackups = (await catalog.GetAllBackupsAsync(cancellationToken))
            .OrderBy(b => b.CreatedAtUtc)
            .ToList();

        if (allBackups.Count == 0) return result;

        var fullBackups = allBackups.Where(b => b.Type == BackupType.Full).ToList();
        var incrementalBackups = allBackups.Where(b => b.Type == BackupType.Incremental).ToList();

        var candidatesForDeletion = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Age Rule (MaxDays)
        if (policy.MaxDays > 0)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-policy.MaxDays);
            foreach (var b in allBackups.Where(b => b.CreatedAtUtc < cutoffDate))
            {
                candidatesForDeletion.Add(b.BackupId);
            }
        }

        // 2. Incremental Count Rule (KeepIncrementalBackups)
        if (policy.KeepIncrementalBackups > 0 && incrementalBackups.Count > policy.KeepIncrementalBackups)
        {
            var excessCount = incrementalBackups.Count - policy.KeepIncrementalBackups;
            var excessInc = incrementalBackups.Take(excessCount);
            foreach (var b in excessInc)
            {
                candidatesForDeletion.Add(b.BackupId);
            }
        }

        // 3. Full Count Rule (KeepFullBackups)
        if (policy.KeepFullBackups > 0 && fullBackups.Count > policy.KeepFullBackups)
        {
            var excessFullCount = fullBackups.Count - policy.KeepFullBackups;
            var excessFull = fullBackups.Take(excessFullCount);
            foreach (var b in excessFull)
            {
                candidatesForDeletion.Add(b.BackupId);
            }
        }

        // 4. CRITICAL INVARIANT: Protect Full backups if dependent incrementals are NOT deleted
        var safeDeletions = new List<BackupCatalogEntry>();

        foreach (var backup in allBackups)
        {
            if (!candidatesForDeletion.Contains(backup.BackupId))
                continue;

            if (backup.Type == BackupType.Full)
            {
                // Check if any incremental depends on this Full and is NOT in candidatesForDeletion
                var dependentIncs = await catalog.GetDependentIncrementalsAsync(backup.BackupId, cancellationToken);
                bool hasActiveDependents = dependentIncs.Any(inc => !candidatesForDeletion.Contains(inc.BackupId));

                if (hasActiveDependents)
                {
                    // PROTECTED: Do not delete Full backup
                    continue;
                }
            }

            safeDeletions.Add(backup);
        }

        // Execute deletions
        foreach (var backup in safeDeletions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = Path.Combine(storage.RootPath, backup.RelativeFilePath);
            storage.DeleteFile(filePath);

            await catalog.DeleteBackupRecordAsync(backup.BackupId, cancellationToken);

            if (backup.Type == BackupType.Full)
                result.FullBackupsRemoved++;
            else
                result.IncrementalBackupsRemoved++;

            result.BytesFreed += backup.CompressedSizeBytes;
            result.RemovedBackupIds.Add(backup.BackupId);
        }

        var retained = allBackups.Where(b => !result.RemovedBackupIds.Contains(b.BackupId)).Select(b => b.BackupId).ToList();
        result.RetainedBackupIds.AddRange(retained);

        return result;
    }
}
