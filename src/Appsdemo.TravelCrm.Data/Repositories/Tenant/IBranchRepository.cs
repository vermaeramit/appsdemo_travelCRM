using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public interface IBranchRepository
{
    Task<IReadOnlyList<Branch>> ListActiveAsync();
    Task<Branch?> GetByIdAsync(Guid id);
}

public sealed class BranchRepository : IBranchRepository
{
    private readonly ITenantConnectionFactory _factory;
    public BranchRepository(ITenantConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Branch>> ListActiveAsync()
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<Branch>(
            @"SELECT id, name, code, address, city, state, country, phone, email, gstin,
                     is_active AS IsActive, is_head_office AS IsHeadOffice, created_at AS CreatedAt
              FROM branches
              WHERE is_active = TRUE AND is_deleted = FALSE
              ORDER BY is_head_office DESC, name");
        return rows.AsList();
    }

    public async Task<Branch?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<Branch>(
            @"SELECT id, name, code, address, city, state, country, phone, email, gstin,
                     is_active AS IsActive, is_head_office AS IsHeadOffice, created_at AS CreatedAt
              FROM branches WHERE id = @id", new { id });
    }
}
