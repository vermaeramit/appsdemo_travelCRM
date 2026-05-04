using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public interface IAuditRepository
{
    Task<PagedResult<AuditLogEntry>> ListAsync(AuditFilter filter);
}

public sealed class AuditFilter
{
    public string? Search { get; set; }
    public string? Entity { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class AuditRepository : IAuditRepository
{
    private readonly ITenantConnectionFactory _factory;
    public AuditRepository(ITenantConnectionFactory factory) => _factory = factory;

    public async Task<PagedResult<AuditLogEntry>> ListAsync(AuditFilter filter)
    {
        using var conn = _factory.Open();
        const string where = @"
            WHERE (@Search IS NULL OR action ILIKE '%'||@Search||'%'
                   OR actor_email ILIKE '%'||@Search||'%' OR entity_id ILIKE '%'||@Search||'%')
              AND (@Entity IS NULL OR entity = @Entity)";

        var sql = $@"
            SELECT id, actor_id AS ActorId, actor_email AS ActorEmail, action, entity,
                   entity_id AS EntityId, payload::text AS Payload, ip, user_agent AS UserAgent,
                   created_at AS CreatedAt
            FROM audit_log {where}
            ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset;
            SELECT COUNT(*) FROM audit_log {where};";

        var args = new { filter.Search, filter.Entity, filter.PageSize, Offset = (filter.Page - 1) * filter.PageSize };
        using var multi = await conn.QueryMultipleAsync(sql, args);
        var items = (await multi.ReadAsync<AuditLogEntry>()).AsList();
        var total = await multi.ReadFirstAsync<int>();
        return new PagedResult<AuditLogEntry> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }
}
