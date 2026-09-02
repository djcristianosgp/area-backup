namespace Area.Backup.Core.Models;

/// <summary>
/// Database backup provider configuration (e.g. Firebird, PostgreSQL).
/// </summary>
public sealed class DatabaseBackupOptions
{
    public bool Enabled { get; init; } = false;
    public string? ProviderType { get; init; } // "Firebird", "PostgreSQL", etc.
    public string? DatabasePath { get; init; }
    public string? Host { get; init; }
    public int Port { get; init; }
    public string? DatabaseName { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? GbakPath { get; init; }
    public string? PgDumpPath { get; init; }
    public Dictionary<string, string> ExtraParameters { get; init; } = new();
}
