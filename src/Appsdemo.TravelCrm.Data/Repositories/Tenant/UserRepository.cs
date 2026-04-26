using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public sealed class UserRepository : IUserRepository
{
    private readonly ITenantConnectionFactory _factory;
    public UserRepository(ITenantConnectionFactory factory) => _factory = factory;

    private const string SelectFields = @"
        id, email, username, full_name AS FullName, password_hash AS PasswordHash,
        phone, branch_id AS BranchId, reports_to_id AS ReportsToId,
        is_active AS IsActive, must_change_password AS MustChangePassword,
        last_login_at AS LastLoginAt, last_login_ip AS LastLoginIp,
        failed_login_count AS FailedLoginCount, locked_until AS LockedUntil,
        two_factor_enabled AS TwoFactorEnabled, two_factor_secret AS TwoFactorSecret,
        profile_image AS ProfileImage,
        created_at AS CreatedAt, created_by AS CreatedBy,
        updated_at AS UpdatedAt, updated_by AS UpdatedBy";

    public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<User>(
            $"SELECT {SelectFields} FROM users WHERE LOWER(email) = LOWER(@v) OR LOWER(username) = LOWER(@v)",
            new { v = emailOrUsername });
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<User>(
            $"SELECT {SelectFields} FROM users WHERE id = @id",
            new { id });
    }

    public async Task<IReadOnlyList<User>> ListAsync(string? search, int page, int pageSize)
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<User>(
            $@"SELECT {SelectFields} FROM users
               WHERE (@search IS NULL OR full_name ILIKE '%' || @search || '%'
                      OR email ILIKE '%' || @search || '%' OR username ILIKE '%' || @search || '%')
               ORDER BY created_at DESC
               LIMIT @pageSize OFFSET @offset",
            new { search, pageSize, offset = (page - 1) * pageSize });
        return rows.AsList();
    }

    public async Task<int> CountAsync(string? search)
    {
        using var conn = _factory.Open();
        return await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM users
              WHERE (@search IS NULL OR full_name ILIKE '%' || @search || '%'
                     OR email ILIKE '%' || @search || '%' OR username ILIKE '%' || @search || '%')",
            new { search });
    }

    public async Task<Guid> InsertAsync(User u)
    {
        using var conn = _factory.Open();
        return await conn.ExecuteScalarAsync<Guid>(@"
            INSERT INTO users (
                id, email, username, full_name, password_hash, phone, branch_id, reports_to_id,
                is_active, must_change_password, two_factor_enabled, profile_image,
                created_at, created_by
            ) VALUES (
                COALESCE(@Id, gen_random_uuid()), LOWER(@Email), @Username, @FullName, @PasswordHash, @Phone, @BranchId, @ReportsToId,
                @IsActive, @MustChangePassword, @TwoFactorEnabled, @ProfileImage,
                NOW(), @CreatedBy
            ) RETURNING id;", u);
    }

    public async Task UpdateAsync(User u)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(@"
            UPDATE users SET
                full_name = @FullName, phone = @Phone, branch_id = @BranchId,
                reports_to_id = @ReportsToId, is_active = @IsActive,
                must_change_password = @MustChangePassword,
                two_factor_enabled = @TwoFactorEnabled, profile_image = @ProfileImage,
                updated_at = NOW(), updated_by = @UpdatedBy
            WHERE id = @Id", u);
    }

    public async Task UpdateLoginSuccessAsync(Guid id, string ip)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(
            @"UPDATE users SET last_login_at = NOW(), last_login_ip = @ip,
              failed_login_count = 0, locked_until = NULL WHERE id = @id",
            new { id, ip });
    }

    public async Task UpdateLoginFailureAsync(Guid id, int newFailedCount, DateTimeOffset? lockUntil)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(
            @"UPDATE users SET failed_login_count = @newFailedCount, locked_until = @lockUntil WHERE id = @id",
            new { id, newFailedCount, lockUntil });
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId)
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<string>(@"
            SELECT DISTINCT rp.permission_key
            FROM user_roles ur
            JOIN role_permissions rp ON rp.role_id = ur.role_id
            WHERE ur.user_id = @userId
            UNION
            SELECT permission_key FROM user_permissions_override
            WHERE user_id = @userId AND grant_type = 'grant'
            EXCEPT
            SELECT permission_key FROM user_permissions_override
            WHERE user_id = @userId AND grant_type = 'deny'", new { userId });
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(Guid userId)
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<Role>(@"
            SELECT r.id, r.name, r.description, r.is_system AS IsSystem, r.created_at AS CreatedAt
            FROM user_roles ur
            JOIN roles r ON r.id = ur.role_id
            WHERE ur.user_id = @userId
            ORDER BY r.name", new { userId });
        return rows.AsList();
    }
}

public sealed class RoleRepository : IRoleRepository
{
    private readonly ITenantConnectionFactory _factory;
    public RoleRepository(ITenantConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Role>> ListAsync()
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<Role>(
            @"SELECT id, name, description, is_system AS IsSystem, created_at AS CreatedAt
              FROM roles ORDER BY name");
        return rows.AsList();
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<Role>(
            @"SELECT id, name, description, is_system AS IsSystem, created_at AS CreatedAt
              FROM roles WHERE id = @id", new { id });
    }

    public async Task<Guid> InsertAsync(Role r)
    {
        using var conn = _factory.Open();
        return await conn.ExecuteScalarAsync<Guid>(
            @"INSERT INTO roles (id, name, description, is_system, created_at)
              VALUES (COALESCE(@Id, gen_random_uuid()), @Name, @Description, @IsSystem, NOW())
              RETURNING id;", r);
    }

    public async Task SetPermissionsAsync(Guid roleId, IEnumerable<string> permissionKeys)
    {
        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();
        await conn.ExecuteAsync("DELETE FROM role_permissions WHERE role_id = @roleId",
            new { roleId }, tx);
        foreach (var key in permissionKeys.Distinct())
        {
            await conn.ExecuteAsync(
                "INSERT INTO role_permissions (role_id, permission_key) VALUES (@roleId, @key)",
                new { roleId, key }, tx);
        }
        tx.Commit();
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid roleId)
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<string>(
            "SELECT permission_key FROM role_permissions WHERE role_id = @roleId",
            new { roleId });
        return rows.AsList();
    }
}
