using Atual.Backup.Core.Interfaces;
using Atual.Backup.Database.Providers;
using Atual.Backup.Infrastructure.ChangeDetection;
using Atual.Backup.Infrastructure.Compression;
using Atual.Backup.Infrastructure.Hashing;
using Atual.Backup.Infrastructure.Manifest;
using Atual.Backup.Infrastructure.Restore;
using Atual.Backup.Infrastructure.Retention;
using Atual.Backup.Infrastructure.Scanning;
using Atual.Backup.Infrastructure.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Atual.Backup.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Atual Backup Engine services into the dependency injection container.
    /// </summary>
    public static IServiceCollection AddAtualBackup(this IServiceCollection services)
    {
        services.AddSingleton<IFileScanner, FileSystemScanner>();
        services.AddSingleton<IChecksumService, Sha256ChecksumService>();
        services.AddSingleton<IChangeDetector, FileSystemChangeDetector>();
        services.AddSingleton<ICompressionProvider, ZipCompressionProvider>();
        services.AddSingleton<IManifestService, JsonManifestService>();
        services.AddSingleton<IIntegrityValidator, IntegrityValidator>();
        services.AddSingleton<IRestoreEngine, RestoreEngine>();
        services.AddSingleton<IRetentionPolicyService, RetentionPolicyService>();
        services.AddSingleton<DatabaseProviderFactory>();
        services.AddTransient<IBackupEngine, BackupEngine>();

        return services;
    }
}
