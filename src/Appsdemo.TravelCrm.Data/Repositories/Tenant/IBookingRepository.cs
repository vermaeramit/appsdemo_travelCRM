using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public interface IBookingRepository
{
    Task<PagedResult<BookingListItem>> ListAsync(BookingFilter filter);
    Task<Booking?> GetByIdAsync(Guid id);
    Task<Guid> InsertAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task SoftDeleteAsync(Guid id, Guid? deletedBy);
    Task UpdateStatusAsync(Guid id, string status, Guid? updatedBy);
}

public sealed class BookingFilter
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class BookingRepository : IBookingRepository
{
    private readonly ITenantConnectionFactory _factory;
    public BookingRepository(ITenantConnectionFactory factory) => _factory = factory;

    public async Task<PagedResult<BookingListItem>> ListAsync(BookingFilter filter)
    {
        using var conn = _factory.Open();
        const string where = @"
            WHERE bk.is_deleted = FALSE
              AND (@Search IS NULL OR bk.booking_no ILIKE '%'||@Search||'%'
                   OR bk.customer_name ILIKE '%'||@Search||'%')
              AND (@Status IS NULL OR bk.status = @Status)";

        var sql = $@"
            SELECT bk.id, bk.booking_no AS BookingNo, bk.quote_id AS QuoteId, q.quote_no AS QuoteNo,
                   bk.customer_name AS CustomerName, bk.travel_start AS TravelStart, bk.travel_end AS TravelEnd,
                   bk.status, bk.total_amount AS TotalAmount, bk.paid_amount AS PaidAmount,
                   bk.balance_amount AS BalanceAmount, bk.currency_code AS CurrencyCode,
                   b.name AS BranchName, bk.created_at AS CreatedAt
            FROM bookings bk
            LEFT JOIN quotes q ON q.id = bk.quote_id
            LEFT JOIN branches b ON b.id = bk.branch_id
            {where} ORDER BY bk.created_at DESC LIMIT @PageSize OFFSET @Offset;
            SELECT COUNT(*) FROM bookings bk {where};";

        var args = new { filter.Search, filter.Status, filter.PageSize, Offset = (filter.Page - 1) * filter.PageSize };
        using var multi = await conn.QueryMultipleAsync(sql, args);
        var items = (await multi.ReadAsync<BookingListItem>()).AsList();
        var total = await multi.ReadFirstAsync<int>();
        return new PagedResult<BookingListItem> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();
        var bk = await conn.QuerySingleOrDefaultAsync<Booking>(@"
            SELECT bk.id, bk.booking_no AS BookingNo, bk.quote_id AS QuoteId, bk.lead_id AS LeadId,
                   bk.customer_name AS CustomerName, bk.travel_start AS TravelStart, bk.travel_end AS TravelEnd,
                   bk.status, bk.total_amount AS TotalAmount, bk.paid_amount AS PaidAmount,
                   bk.balance_amount AS BalanceAmount, bk.currency_code AS CurrencyCode,
                   bk.branch_id AS BranchId, bk.notes,
                   bk.is_deleted AS IsDeleted,
                   bk.created_at AS CreatedAt, bk.created_by AS CreatedBy,
                   bk.updated_at AS UpdatedAt, bk.updated_by AS UpdatedBy
            FROM bookings bk WHERE bk.id = @id AND bk.is_deleted = FALSE", new { id });

        if (bk is null) return null;

        var items = await conn.QueryAsync<BookingItem>(@"
            SELECT id, booking_id AS BookingId, day_no AS DayNo, item_date AS ItemDate,
                   item_type AS ItemType, reference_id AS ReferenceId, description,
                   qty, unit_cost AS UnitCost, unit_sale AS UnitSale, tax_rate AS TaxRate,
                   line_total AS LineTotal, sort_order AS SortOrder
            FROM booking_items WHERE booking_id = @id ORDER BY sort_order, id", new { id });

        bk.Items = items.AsList();
        return bk;
    }

    public async Task<Guid> InsertAsync(Booking bk)
    {
        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var id = await conn.ExecuteScalarAsync<Guid>(@"
                INSERT INTO bookings (booking_no, quote_id, lead_id, customer_name,
                    travel_start, travel_end, status, total_amount, paid_amount, balance_amount,
                    currency_code, branch_id, notes, created_at, created_by)
                VALUES (@BookingNo, @QuoteId, @LeadId, @CustomerName,
                    @TravelStart, @TravelEnd, @Status, @TotalAmount, @PaidAmount, @BalanceAmount,
                    @CurrencyCode, @BranchId, @Notes, NOW(), @CreatedBy)
                RETURNING id;", bk, tx);

            var i = 0;
            foreach (var item in bk.Items)
                await conn.ExecuteAsync(@"
                    INSERT INTO booking_items (booking_id, day_no, item_date, item_type, description,
                        qty, unit_cost, unit_sale, tax_rate, line_total, sort_order)
                    VALUES (@BookingId, @DayNo, @ItemDate, @ItemType, @Description,
                        @Qty, @UnitCost, @UnitSale, @TaxRate, @LineTotal, @SortOrder)",
                    new { BookingId = id, item.DayNo, item.ItemDate, item.ItemType, item.Description,
                          item.Qty, item.UnitCost, item.UnitSale, item.TaxRate, item.LineTotal, SortOrder = i++ }, tx);

            tx.Commit();
            return id;
        }
        catch { tx.Rollback(); throw; }
    }

    public async Task UpdateAsync(Booking bk)
    {
        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(@"
                UPDATE bookings SET
                    customer_name = @CustomerName, travel_start = @TravelStart, travel_end = @TravelEnd,
                    status = @Status, total_amount = @TotalAmount, currency_code = @CurrencyCode,
                    branch_id = @BranchId, notes = @Notes, updated_at = NOW(), updated_by = @UpdatedBy
                WHERE id = @Id", bk, tx);

            await conn.ExecuteAsync("DELETE FROM booking_items WHERE booking_id = @Id", new { bk.Id }, tx);
            var i = 0;
            foreach (var item in bk.Items)
                await conn.ExecuteAsync(@"
                    INSERT INTO booking_items (booking_id, day_no, item_date, item_type, description,
                        qty, unit_cost, unit_sale, tax_rate, line_total, sort_order)
                    VALUES (@BookingId, @DayNo, @ItemDate, @ItemType, @Description,
                        @Qty, @UnitCost, @UnitSale, @TaxRate, @LineTotal, @SortOrder)",
                    new { BookingId = bk.Id, item.DayNo, item.ItemDate, item.ItemType, item.Description,
                          item.Qty, item.UnitCost, item.UnitSale, item.TaxRate, item.LineTotal, SortOrder = i++ }, tx);

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public async Task SoftDeleteAsync(Guid id, Guid? deletedBy)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(
            "UPDATE bookings SET is_deleted=TRUE, deleted_at=NOW(), updated_at=NOW(), updated_by=@deletedBy WHERE id=@id",
            new { id, deletedBy });
    }

    public async Task UpdateStatusAsync(Guid id, string status, Guid? updatedBy)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(
            "UPDATE bookings SET status=@status, updated_at=NOW(), updated_by=@updatedBy WHERE id=@id",
            new { id, status, updatedBy });
    }
}
