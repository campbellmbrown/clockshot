using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

using Dapper;

using Microsoft.Data.Sqlite;

namespace ClockShot.Data.Sqlite;

/// <summary>
/// Opens connections to the ClockShot database and keeps its schema up to date.
/// </summary>
public sealed class SqliteDatabase
{
    // How long a command waits while another connection is writing.
    private const int BusyTimeoutSeconds = 5;

    // The LogicalName that ClockShot.Data.csproj gives the scripts, so it doesn't change with the namespace.
    private const string MigrationPrefix = "Migrations.";

    private readonly string _connectionString;

    public SqliteDatabase(string filePath)
        : this(new SqliteConnectionStringBuilder { DataSource = filePath })
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
    }

    /// <summary>
    /// Uses the database that <paramref name="settings"/> describes, such as one held in memory.
    /// </summary>
    public SqliteDatabase(SqliteConnectionStringBuilder settings)
    {
        _connectionString = new SqliteConnectionStringBuilder(settings.ConnectionString)
        {
            ForeignKeys = true,
            DefaultTimeout = BusyTimeoutSeconds,
            Pooling = true,
        }.ToString();
    }

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Applies the embedded <c>Migrations/NNNN_Name.sql</c> scripts that haven't run yet, in order.
    /// The number of the last one applied is stored in <c>PRAGMA user_version</c>.
    /// </summary>
    [SuppressMessage("Sonar", "S2077", Justification = "PRAGMA can't take parameters. The version is a number.")]
    public void Migrate()
    {
        using var connection = Open();
        // WAL lets the tracker write while the UI reads, without blocking each other. It persists in the file.
        connection.Execute("PRAGMA journal_mode = WAL;");

        var current = connection.ExecuteScalar<long>("PRAGMA user_version;");
        foreach (var (version, sql) in Migrations().Where(m => m.Version > current))
        {
            using var transaction = connection.BeginTransaction();
            connection.Execute(sql, transaction: transaction);
            connection.Execute($"PRAGMA user_version = {version};", transaction: transaction);
            transaction.Commit();
        }
    }

    internal static IEnumerable<(long Version, string Sql)> Migrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(MigrationPrefix, StringComparison.Ordinal))
            .Select(name => (Version: VersionOf(name), Sql: ReadScript(assembly, name)))
            .OrderBy(migration => migration.Version);
    }

    private static long VersionOf(string resourceName)
    {
        var fileName = resourceName[MigrationPrefix.Length..];
        var number = fileName.Split('_')[0];
        return long.Parse(number, CultureInfo.InvariantCulture);
    }

    private static string ReadScript(Assembly assembly, string resourceName)
    {
        using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)!);
        return reader.ReadToEnd();
    }
}
