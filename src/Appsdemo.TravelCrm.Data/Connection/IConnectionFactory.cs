using System.Data;
using Appsdemo.TravelCrm.Core.Multitenancy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Appsdemo.TravelCrm.Data.Connection;

public interface IMasterConnectionFactory
{
    IDbConnection Open();
    string ConnectionString { get; }
    string AdminConnectionString { get; }
}

public interface ITenantConnectionFactory
{
    IDbConnection Open();
    IDbConnection Open(string connectionString);
}

public sealed class MasterConnectionFactory : IMasterConnectionFactory
{
    public string ConnectionString { get; }
    public string AdminConnectionString { get; }

    public MasterConnectionFactory(IConfiguration config)
    {
        ConnectionString = config.GetConnectionString("Master")
            ?? throw new InvalidOperationException("ConnectionStrings:Master is missing.");
        AdminConnectionString = config.GetConnectionString("PgAdmin")
            ?? throw new InvalidOperationException("ConnectionStrings:PgAdmin is missing.");
    }

    public IDbConnection Open()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }
}

public sealed class TenantConnectionFactory : ITenantConnectionFactory
{
    private readonly ITenantContextAccessor _tenant;

    public TenantConnectionFactory(ITenantContextAccessor tenant) => _tenant = tenant;

    public IDbConnection Open()
    {
        var ctx = _tenant.Current
            ?? throw new InvalidOperationException("No tenant resolved for this request.");
        return Open(ctx.ConnectionString);
    }

    public IDbConnection Open(string connectionString)
    {
        var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
    }
}

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext?> _current = new();
    public TenantContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
