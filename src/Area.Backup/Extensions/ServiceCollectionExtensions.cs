using Area.Backup.Core.Interfaces;
using Area.Backup.Database.Providers;
using Area.Backup.Infrastructure.ChangeDetection;
using Area.Backup.Infrastructure.Compression;
using Area.Backup.Infrastructure.Hashing;
using Area.Backup.Infrastructure.Manifest;
using Area.Backup.Infrastructure.Restore;
using Area.Backup.Infrastructure.Retention;
using Area.Backup.Infrastructure.Scanning;
using Area.Backup.Infrastructure.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Area.Backup.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Area Backup Engine services into the dependency injection container.
    /// </summary>
    public static IServiceCollection AddAreaBackup(this IServiceCollection services)
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
