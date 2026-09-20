using Microsoft.Data.Sqlite;

namespace AimranDataScienceLab.Engine.Data;

/// <summary>
/// Creates and manages SQLite connections for the AIMRAN metadata database.
/// Supports local Windows deployment with database stored in AppData.
/// </summary>
public sealed class SqliteConnectionFactory : IDisposable
{
    private readonly string _connectionString;
    private bool _disposed;

    public SqliteConnectionFactory(string? databasePath = null)
    {
        var dbPath = databasePath ?? GetDefaultDatabasePath();

        var dbDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        DatabasePath = dbPath;
    }

    /// <summary>
    /// The full path to the SQLite database file.
    /// </summary>
    public string DatabasePath { get; }

    /// <summary>
    /// Get the default database path based on platform and environment.
    /// Prefers AIMRAN_DB_PATH environment variable (set by MauiProgram for Windows deployment).
    /// Falls back to LocalApplicationData for other scenarios.
    /// </summary>
    private static string GetDefaultDatabasePath()
    {
        // Check if running in Windows desktop deployment with configured AppData path
        var dbPathEnv = Environment.GetEnvironmentVariable("AIMRAN_DB_PATH");
        if (!string.IsNullOrWhiteSpace(dbPathEnv))
        {
            return Path.Combine(dbPathEnv, "aimran-lab.db");
        }

        // Fall back to LocalApplicationData
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AIMRAN Lab",
            "Database"
        );

        return Path.Combine(appDataPath, "aimran-lab.db");
    }

    /// <summary>
    /// Create a new open connection with proper SQLite configuration.
    /// </summary>
    public SqliteConnection CreateConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enable WAL mode for better concurrent read performance
        // WAL (Write-Ahead Logging) allows reads while writes are happening
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA foreign_keys=ON;
            PRAGMA synchronous=NORMAL;
            """;
        cmd.ExecuteNonQuery();

        return connection;
    }

    /// <summary>
    /// Backup the database to the configured backup folder.
    /// Useful for migrations and before updates.
    /// </summary>
    public string CreateBackup()
    {
        try
        {
            var backupDir = Environment.GetEnvironmentVariable("AIMRAN_BACKUP_PATH");
            if (string.IsNullOrWhiteSpace(backupDir))
            {
                backupDir = Path.Combine(
                    Path.GetDirectoryName(DatabasePath) ?? "",
                    "backups"
                );
            }

            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(backupDir, $"aimran-lab_{timestamp}.db");

            File.Copy(DatabasePath, backupPath, overwrite: true);
            return backupPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create database backup: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
