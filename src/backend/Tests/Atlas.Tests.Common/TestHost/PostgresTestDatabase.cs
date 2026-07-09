using System.Text.RegularExpressions;
using Npgsql;

namespace Atlas.Tests.Common.TestHost;

/// <summary>
/// Creates and tears down per-factory Postgres databases so Integration tests never share state.
/// </summary>
public static class PostgresTestDatabase
{
    private static readonly Regex SafeDbName = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public static string BuildIsolatedConnectionString(string baseConnectionString, string databaseName)
    {
        EnsureSafeDatabaseName(databaseName);
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = databaseName
        };
        return builder.ConnectionString;
    }

    public static void EnsureDatabaseExists(string baseConnectionString, string databaseName)
    {
        EnsureSafeDatabaseName(databaseName);
        using NpgsqlConnection admin = OpenAdminConnection(baseConnectionString);
        using NpgsqlCommand exists = admin.CreateCommand();
        exists.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
        exists.Parameters.AddWithValue("name", databaseName);
        object? found = exists.ExecuteScalar();
        if (found is not null)
        {
            return;
        }

        using NpgsqlCommand create = admin.CreateCommand();
        // Identifier validated above; CREATE DATABASE does not accept parameters for the name.
        create.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        create.ExecuteNonQuery();
    }

    public static void DropDatabase(string baseConnectionString, string databaseName)
    {
        EnsureSafeDatabaseName(databaseName);
        try
        {
            using NpgsqlConnection admin = OpenAdminConnection(baseConnectionString);
            using NpgsqlCommand terminate = admin.CreateCommand();
            terminate.CommandText =
                """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @name AND pid <> pg_backend_pid()
                """;
            terminate.Parameters.AddWithValue("name", databaseName);
            terminate.ExecuteNonQuery();

            using NpgsqlCommand drop = admin.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\"";
            drop.ExecuteNonQuery();
        }
        catch
        {
            // Best-effort cleanup; CI runners recycle containers anyway.
        }
    }

    private static NpgsqlConnection OpenAdminConnection(string baseConnectionString)
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = "postgres"
        };
        var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        connection.Open();
        return connection;
    }

    private static void EnsureSafeDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName) || !SafeDbName.IsMatch(databaseName))
        {
            throw new ArgumentException(
                $"Postgres test database name '{databaseName}' is invalid. Use letters, digits, and underscores only.",
                nameof(databaseName));
        }
    }
}
