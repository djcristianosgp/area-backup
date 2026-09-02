using System.Globalization;
using Area.Backup.Core.Enums;
using Area.Backup.Core.Interfaces;
using Area.Backup.Core.Models;
using Area.Backup.Core.Models.Manifest;
using Microsoft.Data.Sqlite;

namespace Area.Backup.Infrastructure.Catalog;

/// <summary>
/// SQLite-backed persistent catalog repository storing backup metadata, file version histories, and parent relationships.
/// </summary>
public sealed class SqliteCatalogRepository : ICatalogRepository
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;
    private bool _initialized;

    public SqliteCatalogRepository(string catalogDbPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogDbPath);
        var dir = Path.GetDirectoryName(catalogDbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = catalogDbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    private async Task<SqliteConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync(cancellationToken);
        }
        else if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        return _connection;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        var conn = await GetOpenConnectionAsync(cancellationToken);

        var sql = """
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;

            CREATE TABLE IF NOT EXISTS Backups (
                BackupId TEXT PRIMARY KEY,
                Type INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                Status INTEGER NOT NULL,
                ParentBackupId TEXT,
                RootFullBackupId TEXT,
                RelativeFilePath TEXT NOT NULL,
                FileCount INTEGER NOT NULL,
                DeletedFileCount INTEGER NOT NULL,
                TotalSizeBytes INTEGER NOT NULL,
                CompressedSizeBytes INTEGER NOT NULL,
                ChecksumSha256 TEXT,
                EngineVersion TEXT NOT NULL,
                FormatVersion INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS BackupSources (
                SourceId TEXT NOT NULL,
                BackupId TEXT NOT NULL,
                OriginalPath TEXT NOT NULL,
                Description TEXT,
                PRIMARY KEY (SourceId, BackupId),
                FOREIGN KEY (BackupId) REFERENCES Backups(BackupId) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS BackupFiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BackupId TEXT NOT NULL,
                SourceId TEXT NOT NULL,
                RelativePath TEXT NOT NULL,
                SizeBytes INTEGER NOT NULL,
                LastWriteTimeUtc TEXT NOT NULL,
                ChecksumSha256 TEXT,
                StoredInBackupId TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL,
                FOREIGN KEY (BackupId) REFERENCES Backups(BackupId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Backups_Type_CreatedAt ON Backups(Type, CreatedAtUtc);
            CREATE INDEX IF NOT EXISTS IX_Backups_ParentId ON Backups(ParentBackupId);
            CREATE INDEX IF NOT EXISTS IX_Backups_RootFullId ON Backups(RootFullBackupId);
            CREATE INDEX IF NOT EXISTS IX_BackupFiles_BackupId ON BackupFiles(BackupId);
            CREATE INDEX IF NOT EXISTS IX_BackupFiles_Source_Path ON BackupFiles(SourceId, RelativePath);
        """;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        _initialized = true;
    }

    public async Task RegisterBackupAsync(
        BackupCatalogEntry entry,
        BackupManifest manifest,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var conn = await GetOpenConnectionAsync(cancellationToken);

        using var transaction = conn.BeginTransaction();

        // 1. Insert Backup Header
        var insertBackupSql = """
            INSERT OR REPLACE INTO Backups (
                BackupId, Type, CreatedAtUtc, Status, ParentBackupId, RootFullBackupId,
                RelativeFilePath, FileCount, DeletedFileCount, TotalSizeBytes, CompressedSizeBytes,
                ChecksumSha256, EngineVersion, FormatVersion
            ) VALUES (
                $id, $type, $createdAt, $status, $parent, $rootFull,
                $relPath, $fileCount, $deletedCount, $totalSize, $compSize,
                $checksum, $engineVer, $formatVer
            );
        """;

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = insertBackupSql;
            cmd.Parameters.AddWithValue("$id", entry.BackupId);
            cmd.Parameters.AddWithValue("$type", (int)entry.Type);
            cmd.Parameters.AddWithValue("$createdAt", entry.CreatedAtUtc.ToString("o", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$status", (int)entry.Status);
            cmd.Parameters.AddWithValue("$parent", (object?)entry.ParentBackupId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rootFull", (object?)entry.RootFullBackupId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$relPath", entry.RelativeFilePath);
            cmd.Parameters.AddWithValue("$fileCount", entry.FileCount);
            cmd.Parameters.AddWithValue("$deletedCount", entry.DeletedFileCount);
            cmd.Parameters.AddWithValue("$totalSize", entry.TotalSizeBytes);
            cmd.Parameters.AddWithValue("$compSize", entry.CompressedSizeBytes);
            cmd.Parameters.AddWithValue("$checksum", (object?)entry.ChecksumSha256 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$engineVer", entry.EngineVersion);
            cmd.Parameters.AddWithValue("$formatVer", entry.FormatVersion);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // 2. Insert Sources
        var insertSourceSql = """
            INSERT OR REPLACE INTO BackupSources (SourceId, BackupId, OriginalPath, Description)
            VALUES ($srcId, $backupId, $origPath, $desc);
        """;

        foreach (var source in manifest.Sources)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = insertSourceSql;
            cmd.Parameters.AddWithValue("$srcId", source.SourceId);
            cmd.Parameters.AddWithValue("$backupId", entry.BackupId);
            cmd.Parameters.AddWithValue("$origPath", source.OriginalPath);
            cmd.Parameters.AddWithValue("$desc", (object?)source.Description ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // 3. Insert Files
        var insertFileSql = """
            INSERT INTO BackupFiles (
                BackupId, SourceId, RelativePath, SizeBytes, LastWriteTimeUtc,
                ChecksumSha256, StoredInBackupId, IsDeleted
            ) VALUES (
                $backupId, $srcId, $relPath, $size, $lastWrite,
                $checksum, $storedId, $isDeleted
            );
        """;

        foreach (var file in manifest.Files)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = insertFileSql;
            cmd.Parameters.AddWithValue("$backupId", entry.BackupId);
            cmd.Parameters.AddWithValue("$srcId", file.SourceId);
            cmd.Parameters.AddWithValue("$relPath", file.RelativePath);
            cmd.Parameters.AddWithValue("$size", file.Size);
            cmd.Parameters.AddWithValue("$lastWrite", file.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$checksum", (object?)file.Sha256 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$storedId", entry.BackupId);
            cmd.Parameters.AddWithValue("$isDeleted", 0);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // 4. Insert Deleted Files
        foreach (var del in manifest.DeletedFiles)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = insertFileSql;
            cmd.Parameters.AddWithValue("$backupId", entry.BackupId);
            cmd.Parameters.AddWithValue("$srcId", del.SourceId);
            cmd.Parameters.AddWithValue("$relPath", del.RelativePath);
            cmd.Parameters.AddWithValue("$size", 0);
            cmd.Parameters.AddWithValue("$lastWrite", del.DeletedAtUtc.ToString("o", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$checksum", DBNull.Value);
            cmd.Parameters.AddWithValue("$storedId", entry.BackupId);
            cmd.Parameters.AddWithValue("$isDeleted", 1);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<BackupCatalogEntry?> GetBackupByIdAsync(string backupId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var conn = await GetOpenConnectionAsync(cancellationToken);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Backups WHERE BackupId = $id LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", backupId);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapCatalogEntry(reader);
        }

        return null;
    }

    public async Task<BackupCatalogEntry?> GetLatestBackupAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var conn = await GetOpenConnectionAsync(cancellationToken);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Backups WHERE Status = 2 ORDER BY CreatedAtUtc DESC LIMIT 1;"; // 2 = Completed

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapCatalogEntry(reader);
        }

        return null;
    }

    public async Task<BackupCatalogEntry?> GetLatestFullBackupAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var conn = await GetOpenConnectionAsync(cancellationToken);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Backups WHERE Type = 1 AND Status = 2 ORDER BY CreatedAtUtc DESC LIMIT 1;"; // 1 = Full

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapCatalogEntry(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<BackupCatalogEntry>> GetAllBackupsAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var conn = await GetOpenConnectionAsync(cancellationToken);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Backups ORDER BY CreatedAtUtc ASC;";

        var list = new List<BackupCatalogEntry>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapCatalogEntry(reader));
        }

        return list;
    }

    public async Task<IReadOnlyList<BackupCatalogEntry>> GetBackupChainAsync(string targetBackupId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var allBackups = await GetAllBackupsAsync(cancellationToken);
        var lookup = allBackups.ToDictionary(b => b.BackupId, StringComparer.OrdinalIgnoreCase);

        if (!lookup.TryGetValue(targetBackupId, out var current))
            return Array.Empty<BackupCatalogEntry>();

        var chain = new List<BackupCatalogEntry> { current };

        // Walk backwards up to the Full backup
        while (current.Type == BackupType.Incremental && !string.IsNullOrEmpty(current.ParentBackupId))
        {
            if (lookup.TryGetValue(current.ParentBackupId, out var parent))
            {
                chain.Insert(0, parent);
                current = parent;
            }
            else
            {
                break;
            }
        }

        return chain;
    }

    public async Task<IReadOnlyList<BackupCatalogEntry>> GetDependentIncrementalsAsync(string fullBackupId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var conn = await GetOpenConnectionAsync(cancellationToken);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Backups WHERE RootFullBackupId = $fullId OR ParentBackupId = $fullId;";
        cmd.Parameters.AddWithValue("$fullId", fullBackupId);

        var list = new List<BackupCatalogEntry>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapCatalogEntry(reader));
        }

        return list;
    }

    public async Task<int> GetIncrementalCountSinceLastFullAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var latestFull = await GetLatestFullBackupAsync(cancellationToken);
        if (latestFull == null) return 0;

        var conn = await GetOpenConnectionAsync(cancellationToken);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM Backups
            WHERE Type = 2 AND Status = 2 AND CreatedAtUtc > $fullDate;
        """;
        cmd.Parameters.AddWithValue("$fullDate", latestFull.CreatedAtUtc.ToString("o", CultureInfo.InvariantCulture));

        var count = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(count);
    }

    public async Task DeleteBackupRecordAsync(string backupId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var conn = await GetOpenConnectionAsync(cancellationToken);

        using var transaction = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = "DELETE FROM BackupFiles WHERE BackupId = $id;";
            cmd.Parameters.AddWithValue("$id", backupId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = "DELETE FROM BackupSources WHERE BackupId = $id;";
            cmd.Parameters.AddWithValue("$id", backupId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = "DELETE FROM Backups WHERE BackupId = $id;";
            cmd.Parameters.AddWithValue("$id", backupId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static BackupCatalogEntry MapCatalogEntry(SqliteDataReader reader)
    {
        return new BackupCatalogEntry
        {
            BackupId = reader.GetString(reader.GetOrdinal("BackupId")),
            Type = (BackupType)reader.GetInt32(reader.GetOrdinal("Type")),
            CreatedAtUtc = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAtUtc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Status = (BackupStatus)reader.GetInt32(reader.GetOrdinal("Status")),
            ParentBackupId = reader.IsDBNull(reader.GetOrdinal("ParentBackupId")) ? null : reader.GetString(reader.GetOrdinal("ParentBackupId")),
            RootFullBackupId = reader.IsDBNull(reader.GetOrdinal("RootFullBackupId")) ? null : reader.GetString(reader.GetOrdinal("RootFullBackupId")),
            RelativeFilePath = reader.GetString(reader.GetOrdinal("RelativeFilePath")),
            FileCount = reader.GetInt64(reader.GetOrdinal("FileCount")),
            DeletedFileCount = reader.GetInt64(reader.GetOrdinal("DeletedFileCount")),
            TotalSizeBytes = reader.GetInt64(reader.GetOrdinal("TotalSizeBytes")),
            CompressedSizeBytes = reader.GetInt64(reader.GetOrdinal("CompressedSizeBytes")),
            ChecksumSha256 = reader.IsDBNull(reader.GetOrdinal("ChecksumSha256")) ? null : reader.GetString(reader.GetOrdinal("ChecksumSha256")),
            EngineVersion = reader.GetString(reader.GetOrdinal("EngineVersion")),
            FormatVersion = reader.GetInt32(reader.GetOrdinal("FormatVersion"))
        };
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
