using System.Reflection;
using DbUp;
using DbUp.Engine;
using Npgsql;

namespace Appsdemo.TravelCrm.Migrations;

public static class MigrationRunner
{
    private static readonly Assembly Asm = typeof(MigrationRunner).Assembly;

    public static MigrationResult RunMaster(string connectionString)
        => Run(connectionString, "Appsdemo.TravelCrm.Migrations.Scripts.Master.", "schema_versions_master");

    public static MigrationResult RunTenant(string connectionString)
        => Run(connectionString, "Appsdemo.TravelCrm.Migrations.Scripts.Tenant.", "schema_versions");

    private static MigrationResult Run(string connectionString, string scriptPrefix, string journalTable)
    {
        EnsureUuidExtensionWillSucceed(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Asm, name => name.StartsWith(scriptPrefix))
            .JournalToPostgresqlTable("public", journalTable)
            .LogToConsole()
            .WithTransactionPerScript()
            .Build();

        var result = upgrader.PerformUpgrade();
        return new MigrationResult(result.Successful, result.Error?.Message,
            result.Scripts.Select(s => s.Name).ToArray());
    }

    public static void EnsureDatabaseExists(string adminConnectionString, string dbName)
    {
        using var conn = new NpgsqlConnection(adminConnectionString);
        conn.Open();
        using var check = conn.CreateCommand();
        check.CommandText = "SELECT 1 FROM pg_database WHERE datname = @n";
        var p = check.CreateParameter();
        p.ParameterName = "@n"; p.Value = dbName;
        check.Parameters.Add(p);
        var exists = check.ExecuteScalar() != null;
        if (exists) return;

        using var create = conn.CreateCommand();
        create.CommandText = $"CREATE DATABASE \"{dbName}\" ENCODING 'UTF8'";
        create.ExecuteNonQuery();
    }

    private static void EnsureUuidExtensionWillSucceed(string connectionString)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS pgcrypto";
        cmd.ExecuteNonQuery();
    }
}

public sealed record MigrationResult(bool Success, string? Error, string[] AppliedScripts);
