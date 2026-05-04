using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public interface IBranchRepository
{
    Task<IReadOnlyList<Branch>> ListActiveAsync();
    Task<IReadOnlyList<Branch>> ListAllAsync();
    Task<Branch?> GetByIdAsync(Guid id);
    Task<Guid> InsertAsync(Branch branch, Guid? createdBy);
    Task UpdateAsync(Branch branch, Guid? updatedBy);
}

public sealed class BranchRepository : IBranchRepository
{
    private readonly ITenantConnectionFactory _factory;
    public BranchRepository(ITenantConnectionFactory factory) => _factory = factory;

    private const string SelectCols = @"
        id, name, code, address, city, state, country, phone, email, gstin,
        is_active AS IsActive, is_head_office AS IsHeadOffice, created_at AS CreatedAt";

    public async Task<IReadOnlyList<Branch>> ListActiveAsync()
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<Branch>(
            $"SELECT {SelectCols} FROM branches WHERE is_active = TRUE AND is_deleted = FALSE ORDER BY is_head_office DESC, name");
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Branch>> ListAllAsync()
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<Branch>(
            $"SELECT {SelectCols} FROM branches WHERE is_deleted = FALSE ORDER BY is_head_office DESC, is_active DESC, name");
        return rows.AsList();
    }

    public async Task<Branch?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<Branch>(
            $"SELECT {SelectCols} FROM branches WHERE id = @id AND is_deleted = FALSE", new { id });
    }

    public async Task<Guid> InsertAsync(Branch b, Guid? createdBy)
    {
        using var conn = _factory.Open();
        return await conn.ExecuteScalarAsync<Guid>(@"
            INSERT INTO branches (name, code, address, city, state, country, phone, email, gstin,
                is_active, is_head_office, created_at, created_by)
            VALUES (@Name, @Code, @Address, @City, @State, @Country, @Phone, @Email, @Gstin,
                @IsActive, @IsHeadOffice, NOW(), @createdBy)
            RETURNING id;", new
        {
            b.Name, b.Code, b.Address, b.City, b.State, b.Country,
            b.Phone, b.Email, b.Gstin, b.IsActive, b.IsHeadOffice, createdBy
        });
    }

    public async Task UpdateAsync(Branch b, Guid? updatedBy)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(@"
            UPDATE branches SET
                name = @Name, code = @Code, address = @Address, city = @City,
                state = @State, country = @Country, phone = @Phone, email = @Email,
                gstin = @Gstin, is_active = @IsActive, is_head_office = @IsHeadOffice,
                updated_at = NOW(), updated_by = @updatedBy
            WHERE id = @Id", new
        {
            b.Id, b.Name, b.Code, b.Address, b.City, b.State, b.Country,
            b.Phone, b.Email, b.Gstin, b.IsActive, b.IsHeadOffice, updatedBy
        });
    }
}
