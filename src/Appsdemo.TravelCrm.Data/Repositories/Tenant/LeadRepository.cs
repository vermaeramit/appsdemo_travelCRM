using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public sealed class LeadRepository : ILeadRepository
{
    private readonly ITenantConnectionFactory _factory;
    public LeadRepository(ITenantConnectionFactory factory) => _factory = factory;

    public async Task<PagedResult<LeadListItem>> ListAsync(LeadFilter filter)
    {
        using var conn = _factory.Open();

        const string whereClause = @"
            WHERE l.is_deleted = FALSE
              AND (@Search IS NULL OR l.customer_name ILIKE '%' || @Search || '%'
                   OR l.lead_no ILIKE '%' || @Search || '%'
                   OR l.email   ILIKE '%' || @Search || '%'
                   OR l.phone   ILIKE '%' || @Search || '%')
              AND (@Status IS NULL OR l.status = @Status)
              AND (@AssignedTo IS NULL OR l.assigned_to = @AssignedTo)
              AND (@BranchId   IS NULL OR l.branch_id   = @BranchId)";

        var sql = $@"
            SELECT l.id, l.lead_no AS LeadNo, l.customer_name AS CustomerName,
                   l.email, l.phone, l.status, l.travel_date AS TravelDate,
                   l.created_at AS CreatedAt,
                   u.full_name AS AssignedToName,
                   b.name AS BranchName
            FROM leads l
            LEFT JOIN users u ON u.id = l.assigned_to
            LEFT JOIN branches b ON b.id = l.branch_id
            {whereClause}
            ORDER BY l.created_at DESC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM leads l {whereClause};";

        var args = new
        {
            filter.Search, filter.Status, filter.AssignedTo, filter.BranchId,
            filter.PageSize,
            Offset = (filter.Page - 1) * filter.PageSize
        };

        using var multi = await conn.QueryMultipleAsync(sql, args);
        var items = (await multi.ReadAsync<LeadListItem>()).AsList();
        var total = await multi.ReadFirstAsync<int>();

        return new PagedResult<LeadListItem>
        {
            Items = items, TotalCount = total,
            Page = filter.Page, PageSize = filter.PageSize
        };
    }

    public async Task<Lead?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<Lead>(@"
            SELECT id, lead_no AS LeadNo, source, customer_name AS CustomerName,
                   email, phone, destination_id AS DestinationId,
                   travel_date AS TravelDate, travel_nights AS TravelNights,
                   pax_adults AS PaxAdults, pax_children AS PaxChildren,
                   budget, currency_code AS CurrencyCode, status,
                   lost_reason AS LostReason, assigned_to AS AssignedTo,
                   branch_id AS BranchId, notes,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM leads WHERE id = @id AND is_deleted = FALSE", new { id });
    }

    public async Task<Guid> InsertAsync(Lead l)
    {
        using var conn = _factory.Open();
        return await conn.ExecuteScalarAsync<Guid>(@"
            INSERT INTO leads (
                lead_no, source, customer_name, email, phone, destination_id,
                travel_date, travel_nights, pax_adults, pax_children, budget,
                currency_code, status, assigned_to, branch_id, notes,
                created_at, created_by
            ) VALUES (
                @LeadNo, @Source, @CustomerName, @Email, @Phone, @DestinationId,
                @TravelDate, @TravelNights, @PaxAdults, @PaxChildren, @Budget,
                @CurrencyCode, @Status, @AssignedTo, @BranchId, @Notes,
                NOW(), @CreatedBy
            ) RETURNING id;", l);
    }

    public async Task UpdateAsync(Lead l)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(@"
            UPDATE leads SET
                source = @Source, customer_name = @CustomerName,
                email = @Email, phone = @Phone, destination_id = @DestinationId,
                travel_date = @TravelDate, travel_nights = @TravelNights,
                pax_adults = @PaxAdults, pax_children = @PaxChildren,
                budget = @Budget, currency_code = @CurrencyCode,
                status = @Status, lost_reason = @LostReason,
                assigned_to = @AssignedTo, branch_id = @BranchId,
                notes = @Notes,
                updated_at = NOW(), updated_by = @UpdatedBy
            WHERE id = @Id", l);
    }

    public async Task SoftDeleteAsync(Guid id, Guid? deletedBy)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(@"
            UPDATE leads
            SET is_deleted = TRUE, deleted_at = NOW(),
                updated_at = NOW(), updated_by = @deletedBy
            WHERE id = @id", new { id, deletedBy });
    }

    public async Task<IReadOnlyList<LeadFollowup>> ListFollowupsAsync(Guid leadId)
    {
        using var conn = _factory.Open();
        var rows = await conn.QueryAsync<LeadFollowup>(@"
            SELECT f.id, f.lead_id AS LeadId, f.followup_date AS FollowupDate,
                   f.mode, f.notes, f.next_followup_date AS NextFollowupDate,
                   f.done_by AS DoneBy, u.full_name AS DoneByName,
                   f.created_at AS CreatedAt
            FROM lead_followups f
            LEFT JOIN users u ON u.id = f.done_by
            WHERE f.lead_id = @leadId
            ORDER BY f.followup_date DESC, f.created_at DESC",
            new { leadId });
        return rows.AsList();
    }

    public async Task<Guid> InsertFollowupAsync(LeadFollowup f)
    {
        using var conn = _factory.Open();
        return await conn.ExecuteScalarAsync<Guid>(@"
            INSERT INTO lead_followups
                (lead_id, followup_date, mode, notes, next_followup_date, done_by, created_at)
            VALUES
                (@LeadId, @FollowupDate, @Mode, @Notes, @NextFollowupDate, @DoneBy, NOW())
            RETURNING id;", f);
    }
}
