using Appsdemo.TravelCrm.Core.Models.Master;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Core.Security;
using Appsdemo.TravelCrm.Data.Connection;
using Appsdemo.TravelCrm.Data.Repositories.Master;
using Appsdemo.TravelCrm.Data.Security;
using Appsdemo.TravelCrm.Migrations;
using Appsdemo.TravelCrm.Web.Configuration;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Appsdemo.TravelCrm.Web.Services;

public interface ITenantProvisioner
{
    Task<Tenant> ProvisionAsync(NewTenantRequest request, Guid createdBy);
}

public sealed record NewTenantRequest(
    string Code,
    string CompanyName,
    string ContactPerson,
    string Email,
    string Phone,
    string Country,
    string Timezone,
    string CurrencyCode,
    Guid PlanId,
    string AdminUserFullName,
    string AdminUserEmail,
    string AdminUserPassword);

public sealed class TenantProvisioner : ITenantProvisioner
{
    private readonly ITenantRepository _tenants;
    private readonly ISubscriptionPlanRepository _plans;
    private readonly IMasterConnectionFactory _master;
    private readonly TenancyOptions _opt;
    private readonly ISecretCipher _cipher;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<TenantProvisioner> _log;

    public TenantProvisioner(
        ITenantRepository tenants,
        ISubscriptionPlanRepository plans,
        IMasterConnectionFactory master,
        IOptions<TenancyOptions> opt,
        ISecretCipher cipher,
        IPasswordHasher hasher,
        ILogger<TenantProvisioner> log)
    {
        _tenants = tenants;
        _plans = plans;
        _master = master;
        _opt = opt.Value;
        _cipher = cipher;
        _hasher = hasher;
        _log = log;
    }

    public async Task<Tenant> ProvisionAsync(NewTenantRequest req, Guid createdBy)
    {
        var existing = await _tenants.GetByCodeAsync(req.Code);
        if (existing is not null)
            throw new InvalidOperationException($"Tenant code '{req.Code}' already exists.");

        var plan = await _plans.GetByIdAsync(req.PlanId)
                   ?? throw new InvalidOperationException("Plan not found.");

        var dbName = $"{_opt.TenantDbPrefix}{req.Code.ToLowerInvariant()}";
        var encryptedPassword = _cipher.Encrypt(_opt.TenantDbPassword);

        var tenant = new Tenant
        {
            Code = req.Code.ToLowerInvariant(),
            CompanyName = req.CompanyName,
            ContactPerson = req.ContactPerson,
            Email = req.Email,
            Phone = req.Phone,
            Country = string.IsNullOrWhiteSpace(req.Country) ? "India" : req.Country,
            Timezone = string.IsNullOrWhiteSpace(req.Timezone) ? "Asia/Kolkata" : req.Timezone,
            CurrencyCode = string.IsNullOrWhiteSpace(req.CurrencyCode) ? "INR" : req.CurrencyCode,
            DbName = dbName,
            DbHost = _opt.TenantDbHost,
            DbPort = _opt.TenantDbPort,
            DbUser = _opt.TenantDbUser,
            DbPasswordEncrypted = encryptedPassword,
            PlanId = plan.Id,
            Status = "Trial",
            TrialEndsOn = DateTime.UtcNow.Date.AddDays(plan.TrialDays),
            MaxUsers = plan.MaxUsers,
            CreatedBy = createdBy
        };

        // 1) Create the tenant DB on the same Postgres server
        MigrationRunner.EnsureDatabaseExists(_master.AdminConnectionString, dbName);
        _log.LogInformation("Created tenant database {DbName}", dbName);

        // 2) Run tenant migrations
        var tenantConnStr = $"Host={tenant.DbHost};Port={tenant.DbPort};Database={tenant.DbName};Username={tenant.DbUser};Password={_opt.TenantDbPassword}";
        var migration = MigrationRunner.RunTenant(tenantConnStr);
        if (!migration.Success)
            throw new InvalidOperationException($"Migration failed: {migration.Error}");

        // 3) Seed the tenant-admin user + assign TenantAdmin role
        await SeedAdminAndRolesAsync(tenantConnStr, req);

        // 4) Insert tenant row in master
        tenant.Id = await _tenants.InsertAsync(tenant);
        return tenant;
    }

    private async Task SeedAdminAndRolesAsync(string connStr, NewTenantRequest req)
    {
        using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        var hash = _hasher.Hash(req.AdminUserPassword);
        var userId = await conn.ExecuteScalarAsync<Guid>(@"
            INSERT INTO users (email, username, full_name, password_hash, is_active, must_change_password)
            VALUES (LOWER(@email), @username, @fullName, @hash, TRUE, TRUE)
            RETURNING id;",
            new
            {
                email = req.AdminUserEmail,
                username = req.AdminUserEmail.Split('@')[0],
                fullName = req.AdminUserFullName,
                hash
            }, tx);

        var roleId = await conn.ExecuteScalarAsync<Guid>(
            "SELECT id FROM roles WHERE name = 'TenantAdmin'", transaction: tx);

        await conn.ExecuteAsync(
            "INSERT INTO user_roles (user_id, role_id) VALUES (@userId, @roleId) ON CONFLICT DO NOTHING",
            new { userId, roleId }, tx);

        // Seed initial role-permissions for non-admin system roles based on a sensible default
        await SeedDefaultRolePermissionsAsync(conn, tx);

        await tx.CommitAsync();
    }

    private static async Task SeedDefaultRolePermissionsAsync(
        NpgsqlConnection conn,
        System.Data.Common.DbTransaction tx)
    {
        var defaults = new Dictionary<string, string[]>
        {
            ["Manager"] = new[]
            {
                Permissions.Dashboard.View,
                Permissions.Leads.View, Permissions.Leads.Create, Permissions.Leads.Edit, Permissions.Leads.Assign, Permissions.Leads.Export,
                Permissions.Quotes.View, Permissions.Quotes.Create, Permissions.Quotes.Edit, Permissions.Quotes.Approve, Permissions.Quotes.Send, Permissions.Quotes.Export,
                Permissions.Bookings.View, Permissions.Bookings.Create, Permissions.Bookings.Edit, Permissions.Bookings.Cancel, Permissions.Bookings.Export,
                Permissions.Vouchers.View, Permissions.Vouchers.Create, Permissions.Vouchers.Edit, Permissions.Vouchers.Send,
                Permissions.Invoices.View, Permissions.Invoices.Create, Permissions.Invoices.Edit, Permissions.Invoices.Send, Permissions.Invoices.Export,
                Permissions.Payments.View, Permissions.Payments.Create,
                Permissions.Masters.View, Permissions.Masters.Manage,
                Permissions.Reports.View, Permissions.Reports.Export,
                Permissions.Users.View, Permissions.Branches.View
            },
            ["Sales"] = new[]
            {
                Permissions.Dashboard.View,
                Permissions.Leads.View, Permissions.Leads.Create, Permissions.Leads.Edit,
                Permissions.Quotes.View, Permissions.Quotes.Create, Permissions.Quotes.Edit, Permissions.Quotes.Send,
                Permissions.Masters.View
            },
            ["Ops"] = new[]
            {
                Permissions.Dashboard.View,
                Permissions.Bookings.View, Permissions.Bookings.Create, Permissions.Bookings.Edit,
                Permissions.Vouchers.View, Permissions.Vouchers.Create, Permissions.Vouchers.Edit, Permissions.Vouchers.Send,
                Permissions.Masters.View, Permissions.Masters.Manage
            },
            ["Accounts"] = new[]
            {
                Permissions.Dashboard.View,
                Permissions.Invoices.View, Permissions.Invoices.Create, Permissions.Invoices.Edit, Permissions.Invoices.Send, Permissions.Invoices.Export,
                Permissions.Payments.View, Permissions.Payments.Create, Permissions.Payments.Edit,
                Permissions.Reports.View, Permissions.Reports.Export
            },
            ["ReadOnly"] = new[]
            {
                Permissions.Dashboard.View,
                Permissions.Leads.View, Permissions.Quotes.View, Permissions.Bookings.View,
                Permissions.Vouchers.View, Permissions.Invoices.View, Permissions.Payments.View,
                Permissions.Masters.View, Permissions.Reports.View
            }
        };

        foreach (var (roleName, perms) in defaults)
        {
            var roleId = await conn.ExecuteScalarAsync<Guid?>(
                "SELECT id FROM roles WHERE name = @roleName", new { roleName }, tx);
            if (roleId is null) continue;

            foreach (var perm in perms)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO role_permissions (role_id, permission_key) VALUES (@roleId, @perm) ON CONFLICT DO NOTHING",
                    new { roleId, perm }, tx);
            }
        }
    }
}
