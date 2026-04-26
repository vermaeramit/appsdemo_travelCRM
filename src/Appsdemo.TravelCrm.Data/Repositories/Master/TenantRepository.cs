using Appsdemo.TravelCrm.Core.Models.Master;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;
using MasterTenant = Appsdemo.TravelCrm.Core.Models.Master.Tenant;

namespace Appsdemo.TravelCrm.Data.Repositories.Master;

public sealed class TenantRepository : ITenantRepository
{
    private readonly IMasterConnectionFactory _factory;

    public TenantRepository(IMasterConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<MasterTenant>> ListAsync(string? search, int page, int pageSize)
    {
        using var conn = _factory.Open();
        var sql = @"
            SELECT id, code, company_name AS CompanyName, contact_person AS ContactPerson,
                   email, phone, country, timezone, currency_code AS CurrencyCode,
                   db_name AS DbName, db_host AS DbHost, db_port AS DbPort, db_user AS DbUser,
                   db_password_encrypted AS DbPasswordEncrypted, plan_id AS PlanId,
                   status, trial_ends_on AS TrialEndsOn,
                   subscription_starts_on AS SubscriptionStartsOn,
                   subscription_ends_on AS SubscriptionEndsOn,
                   max_users AS MaxUsers, logo_path AS LogoPath,
                   created_at AS CreatedAt, created_by AS CreatedBy
            FROM tenants
            WHERE (@search IS NULL OR company_name ILIKE '%' || @search || '%' OR code ILIKE '%' || @search || '%')
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset";
        var rows = await conn.QueryAsync<MasterTenant>(sql, new
        {
            search,
            pageSize,
            offset = (page - 1) * pageSize
        });
        return rows.AsList();
    }

    public async Task<int> CountAsync(string? search)
    {
        using var conn = _factory.Open();
        return await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM tenants
              WHERE (@search IS NULL OR company_name ILIKE '%' || @search || '%' OR code ILIKE '%' || @search || '%')",
            new { search });
    }

    public async Task<MasterTenant?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<MasterTenant>(
            @"SELECT id, code, company_name AS CompanyName, contact_person AS ContactPerson,
                     email, phone, country, timezone, currency_code AS CurrencyCode,
                     db_name AS DbName, db_host AS DbHost, db_port AS DbPort, db_user AS DbUser,
                     db_password_encrypted AS DbPasswordEncrypted, plan_id AS PlanId,
                     status, trial_ends_on AS TrialEndsOn,
                     subscription_starts_on AS SubscriptionStartsOn,
                     subscription_ends_on AS SubscriptionEndsOn,
                     max_users AS MaxUsers, logo_path AS LogoPath,
                     created_at AS CreatedAt, created_by AS CreatedBy
              FROM tenants WHERE id = @id",
            new { id });
    }

    public async Task<MasterTenant?> GetByCodeAsync(string code)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<MasterTenant>(
            @"SELECT id, code, company_name AS CompanyName, contact_person AS ContactPerson,
                     email, phone, country, timezone, currency_code AS CurrencyCode,
                     db_name AS DbName, db_host AS DbHost, db_port AS DbPort, db_user AS DbUser,
                     db_password_encrypted AS DbPasswordEncrypted, plan_id AS PlanId,
                     status, trial_ends_on AS TrialEndsOn,
                     subscription_starts_on AS SubscriptionStartsOn,
                     subscription_ends_on AS SubscriptionEndsOn,
                     max_users AS MaxUsers, logo_path AS LogoPath,
                     created_at AS CreatedAt, created_by AS CreatedBy
              FROM tenants WHERE LOWER(code) = LOWER(@code)",
            new { code });
    }

    public async Task<Guid> InsertAsync(MasterTenant t)
    {
        using var conn = _factory.Open();
        var id = await conn.ExecuteScalarAsync<Guid>(@"
            INSERT INTO tenants (
                id, code, company_name, contact_person, email, phone, country, timezone,
                currency_code, db_name, db_host, db_port, db_user, db_password_encrypted,
                plan_id, status, trial_ends_on, subscription_starts_on, subscription_ends_on,
                max_users, logo_path, created_at, created_by
            ) VALUES (
                COALESCE(NULLIF(@Id, '00000000-0000-0000-0000-000000000000'), gen_random_uuid()), LOWER(@Code), @CompanyName, @ContactPerson, @Email, @Phone, @Country, @Timezone,
                @CurrencyCode, @DbName, @DbHost, @DbPort, @DbUser, @DbPasswordEncrypted,
                @PlanId, @Status, @TrialEndsOn, @SubscriptionStartsOn, @SubscriptionEndsOn,
                @MaxUsers, @LogoPath, NOW(), @CreatedBy
            ) RETURNING id;", t);
        return id;
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(
            "UPDATE tenants SET status = @status WHERE id = @id",
            new { id, status });
    }

    public async Task<IReadOnlyDictionary<string, string>> GetFeaturesForPlanAsync(Guid planId)
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<(string FeatureKey, string FeatureValue)>(
            "SELECT feature_key, feature_value FROM plan_features WHERE plan_id = @planId",
            new { planId });
        return rows.ToDictionary(r => r.FeatureKey, r => r.FeatureValue);
    }
}

public sealed class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly IMasterConnectionFactory _factory;
    public SubscriptionPlanRepository(IMasterConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<SubscriptionPlan>> ListActiveAsync()
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<SubscriptionPlan>(
            @"SELECT id, name, description,
                     price_monthly AS PriceMonthly, price_yearly AS PriceYearly,
                     max_users AS MaxUsers, max_branches AS MaxBranches,
                     trial_days AS TrialDays, is_active AS IsActive, sort_order AS SortOrder
              FROM subscription_plans
              WHERE is_active = true
              ORDER BY sort_order");
        return rows.AsList();
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<SubscriptionPlan>(
            @"SELECT id, name, description,
                     price_monthly AS PriceMonthly, price_yearly AS PriceYearly,
                     max_users AS MaxUsers, max_branches AS MaxBranches,
                     trial_days AS TrialDays, is_active AS IsActive, sort_order AS SortOrder
              FROM subscription_plans WHERE id = @id",
            new { id });
    }
}

public sealed class GlobalUserRepository : IGlobalUserRepository
{
    private readonly IMasterConnectionFactory _factory;
    public GlobalUserRepository(IMasterConnectionFactory factory) => _factory = factory;

    public async Task<GlobalUser?> GetByEmailAsync(string email)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<GlobalUser>(
            @"SELECT id, email, full_name AS FullName, password_hash AS PasswordHash,
                     is_active AS IsActive, last_login_at AS LastLoginAt,
                     failed_login_count AS FailedLoginCount, locked_until AS LockedUntil,
                     two_factor_enabled AS TwoFactorEnabled, two_factor_secret AS TwoFactorSecret,
                     created_at AS CreatedAt
              FROM global_users WHERE LOWER(email) = LOWER(@email)",
            new { email });
    }

    public async Task UpdateLoginSuccessAsync(Guid id)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(
            @"UPDATE global_users
              SET last_login_at = NOW(), failed_login_count = 0, locked_until = NULL
              WHERE id = @id",
            new { id });
    }

    public async Task UpdateLoginFailureAsync(Guid id, int newFailedCount, DateTimeOffset? lockUntil)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(
            @"UPDATE global_users
              SET failed_login_count = @newFailedCount, locked_until = @lockUntil
              WHERE id = @id",
            new { id, newFailedCount, lockUntil });
    }
}
