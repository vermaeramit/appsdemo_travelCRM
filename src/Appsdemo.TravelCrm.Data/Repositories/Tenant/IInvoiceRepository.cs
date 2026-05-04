using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public interface IInvoiceRepository
{
    Task<PagedResult<InvoiceListItem>> ListAsync(InvoiceFilter filter);
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<Guid> InsertAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
    Task SoftDeleteAsync(Guid id, Guid? deletedBy);
    Task UpdateStatusAsync(Guid id, string status, Guid? updatedBy);
}

public sealed class InvoiceFilter
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly ITenantConnectionFactory _factory;
    public InvoiceRepository(ITenantConnectionFactory factory) => _factory = factory;

    public async Task<PagedResult<InvoiceListItem>> ListAsync(InvoiceFilter filter)
    {
        using var conn = _factory.Open();
        const string where = @"
            WHERE inv.is_deleted = FALSE
              AND (@Search IS NULL OR inv.invoice_no ILIKE '%'||@Search||'%'
                   OR inv.customer_name ILIKE '%'||@Search||'%')
              AND (@Status IS NULL OR inv.status = @Status)";

        var sql = $@"
            SELECT inv.id, inv.invoice_no AS InvoiceNo, inv.booking_id AS BookingId,
                   bk.booking_no AS BookingNo, inv.customer_name AS CustomerName,
                   inv.invoice_date AS InvoiceDate, inv.due_date AS DueDate,
                   inv.grand_total AS GrandTotal, inv.currency_code AS CurrencyCode,
                   inv.status, b.name AS BranchName, inv.created_at AS CreatedAt
            FROM invoices inv
            LEFT JOIN bookings bk ON bk.id = inv.booking_id
            LEFT JOIN branches b ON b.id = inv.branch_id
            {where} ORDER BY inv.created_at DESC LIMIT @PageSize OFFSET @Offset;
            SELECT COUNT(*) FROM invoices inv {where};";

        var args = new { filter.Search, filter.Status, filter.PageSize, Offset = (filter.Page - 1) * filter.PageSize };
        using var multi = await conn.QueryMultipleAsync(sql, args);
        var items = (await multi.ReadAsync<InvoiceListItem>()).AsList();
        var total = await multi.ReadFirstAsync<int>();
        return new PagedResult<InvoiceListItem> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<Invoice?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();
        var inv = await conn.QuerySingleOrDefaultAsync<Invoice>(@"
            SELECT id, invoice_no AS InvoiceNo, booking_id AS BookingId,
                   customer_name AS CustomerName, customer_gstin AS CustomerGstin,
                   invoice_date AS InvoiceDate, due_date AS DueDate,
                   sub_total AS SubTotal, tax_total AS TaxTotal, grand_total AS GrandTotal,
                   currency_code AS CurrencyCode, status, branch_id AS BranchId, notes,
                   is_deleted AS IsDeleted,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM invoices WHERE id = @id AND is_deleted = FALSE", new { id });

        if (inv is null) return null;

        var items = await conn.QueryAsync<InvoiceItem>(@"
            SELECT id, invoice_id AS InvoiceId, description, hsn_sac AS HsnSac,
                   qty, unit_price AS UnitPrice, tax_rate AS TaxRate,
                   line_total AS LineTotal, sort_order AS SortOrder
            FROM invoice_items WHERE invoice_id = @id ORDER BY sort_order, id", new { id });

        inv.Items = items.AsList();
        return inv;
    }

    public async Task<Guid> InsertAsync(Invoice inv)
    {
        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var id = await conn.ExecuteScalarAsync<Guid>(@"
                INSERT INTO invoices (invoice_no, booking_id, customer_name, customer_gstin,
                    invoice_date, due_date, sub_total, tax_total, grand_total,
                    currency_code, status, branch_id, notes, created_at, created_by)
                VALUES (@InvoiceNo, @BookingId, @CustomerName, @CustomerGstin,
                    @InvoiceDate, @DueDate, @SubTotal, @TaxTotal, @GrandTotal,
                    @CurrencyCode, @Status, @BranchId, @Notes, NOW(), @CreatedBy)
                RETURNING id;", inv, tx);

            var i = 0;
            foreach (var item in inv.Items)
                await conn.ExecuteAsync(@"
                    INSERT INTO invoice_items (invoice_id, description, hsn_sac, qty, unit_price, tax_rate, line_total, sort_order)
                    VALUES (@InvoiceId, @Description, @HsnSac, @Qty, @UnitPrice, @TaxRate, @LineTotal, @SortOrder)",
                    new { InvoiceId = id, item.Description, item.HsnSac, item.Qty, item.UnitPrice, item.TaxRate, item.LineTotal, SortOrder = i++ }, tx);

            tx.Commit();
            return id;
        }
        catch { tx.Rollback(); throw; }
    }

    public async Task UpdateAsync(Invoice inv)
    {
        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(@"
                UPDATE invoices SET
                    booking_id = @BookingId, customer_name = @CustomerName, customer_gstin = @CustomerGstin,
                    invoice_date = @InvoiceDate, due_date = @DueDate,
                    sub_total = @SubTotal, tax_total = @TaxTotal, grand_total = @GrandTotal,
                    currency_code = @CurrencyCode, status = @Status, branch_id = @BranchId,
                    notes = @Notes, updated_at = NOW(), updated_by = @UpdatedBy
                WHERE id = @Id", inv, tx);

            await conn.ExecuteAsync("DELETE FROM invoice_items WHERE invoice_id = @Id", new { inv.Id }, tx);
            var i = 0;
            foreach (var item in inv.Items)
                await conn.ExecuteAsync(@"
                    INSERT INTO invoice_items (invoice_id, description, hsn_sac, qty, unit_price, tax_rate, line_total, sort_order)
                    VALUES (@InvoiceId, @Description, @HsnSac, @Qty, @UnitPrice, @TaxRate, @LineTotal, @SortOrder)",
                    new { InvoiceId = inv.Id, item.Description, item.HsnSac, item.Qty, item.UnitPrice, item.TaxRate, item.LineTotal, SortOrder = i++ }, tx);

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public async Task SoftDeleteAsync(Guid id, Guid? deletedBy)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(
            "UPDATE invoices SET is_deleted=TRUE, deleted_at=NOW(), updated_at=NOW(), updated_by=@deletedBy WHERE id=@id",
            new { id, deletedBy });
    }

    public async Task UpdateStatusAsync(Guid id, string status, Guid? updatedBy)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(
            "UPDATE invoices SET status=@status, updated_at=NOW(), updated_by=@updatedBy WHERE id=@id",
            new { id, status, updatedBy });
    }
}
