using Appsdemo.TravelCrm.Migrations;
using Appsdemo.TravelCrm.Web.Configuration;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Appsdemo.TravelCrm.Web.Services;

public interface IBootstrapper
{
    Task EnsureMasterAsync();
    Task EnsureSuperAdminAsync();
}

public sealed class Bootstrapper : IBootstrapper
{
    private readonly Data.Connection.IMasterConnectionFactory _master;
    private readonly IPasswordHasher _hasher;
    private readonly SuperAdminOptions _saOpt;
    private readonly ILogger<Bootstrapper> _log;

    public Bootstrapper(
        Data.Connection.IMasterConnectionFactory master,
        IPasswordHasher hasher,
        IOptions<SuperAdminOptions> saOpt,
        ILogger<Bootstrapper> log)
    {
        _master = master;
        _hasher = hasher;
        _saOpt = saOpt.Value;
        _log = log;
    }

    public Task EnsureMasterAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(_master.ConnectionString);
        var dbName = builder.Database!;
        MigrationRunner.EnsureDatabaseExists(_master.AdminConnectionString, dbName);
        var result = MigrationRunner.RunMaster(_master.ConnectionString);
        if (!result.Success)
            throw new InvalidOperationException($"Master migration failed: {result.Error}");
        _log.LogInformation("Master DB ready ({Count} script(s) applied)", result.AppliedScripts.Length);
        return Task.CompletedTask;
    }

    public async Task EnsureSuperAdminAsync()
    {
        if (string.IsNullOrWhiteSpace(_saOpt.Email)) return;
        using var conn = _master.Open();
        var hash = _hasher.Hash(_saOpt.InitialPassword);
        var updated = await conn.ExecuteAsync(@"
            UPDATE global_users
            SET full_name = @FullName,
                password_hash = @hash,
                is_active = TRUE,
                failed_login_count = 0,
                locked_until = NULL
            WHERE LOWER(email) = LOWER(@Email)",
            new { _saOpt.Email, _saOpt.FullName, hash });
        if (updated > 0)
        {
            _log.LogInformation("Updated super-admin {Email}", _saOpt.Email);
            return;
        }

        await conn.ExecuteAsync(@"
            INSERT INTO global_users (email, full_name, password_hash, is_active)
            VALUES (LOWER(@Email), @FullName, @hash, TRUE)",
            new { _saOpt.Email, _saOpt.FullName, hash });
        _log.LogInformation("Seeded super-admin {Email}", _saOpt.Email);
    }
}
