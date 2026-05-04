using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public sealed class QuoteRepository : IQuoteRepository
{
    private readonly ITenantConnectionFactory _factory;
    public QuoteRepository(ITenantConnectionFactory factory) => _factory = factory;

    // ---------------- LIST ----------------
    public async Task<PagedResult<QuoteListItem>> ListAsync(QuoteFilter filter)
    {
        using var conn = _factory.Open();

        const string where = @"
            WHERE q.is_deleted = FALSE
              AND (@Search IS NULL
                   OR q.quote_no ILIKE '%' || @Search || '%'
                   OR COALESCE(q.customer_name, l.customer_name) ILIKE '%' || @Search || '%')
              AND (@Status IS NULL OR q.status = @Status)
              AND (@LeadId IS NULL OR q.lead_id = @LeadId)";

        var sql = $@"
            SELECT q.id, q.quote_no AS QuoteNo, q.lead_id AS LeadId,
                   l.lead_no AS LeadNo,
                   COALESCE(q.customer_name, l.customer_name) AS CustomerName,
                   q.version, q.valid_till AS ValidTill, q.status,
                   q.grand_total AS GrandTotal, q.currency_code AS CurrencyCode,
                   b.name AS BranchName, q.created_at AS CreatedAt
            FROM quotes q
            LEFT JOIN leads  l ON l.id = q.lead_id
            LEFT JOIN branches b ON b.id = q.branch_id
            {where}
            ORDER BY q.created_at DESC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*)
            FROM quotes q
            LEFT JOIN leads l ON l.id = q.lead_id
            {where};";

        var args = new
        {
            filter.Search, filter.Status, filter.LeadId,
            filter.PageSize, Offset = (filter.Page - 1) * filter.PageSize
        };

        using var multi = await conn.QueryMultipleAsync(sql, args);
        var items = (await multi.ReadAsync<QuoteListItem>()).AsList();
        var total = await multi.ReadFirstAsync<int>();

        return new PagedResult<QuoteListItem>
        {
            Items = items, TotalCount = total,
            Page = filter.Page, PageSize = filter.PageSize
        };
    }

    // ---------------- GET BY ID ----------------
    public async Task<Quote?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();

        var quote = await conn.QuerySingleOrDefaultAsync<Quote>(@"
            SELECT q.id, q.quote_no AS QuoteNo, q.lead_id AS LeadId,
                   COALESCE(q.customer_name, l.customer_name) AS CustomerName,
                   q.version, q.valid_till AS ValidTill, q.status,
                   q.total_cost AS TotalCost, q.total_sale AS TotalSale,
                   q.total_tax AS TotalTax, q.grand_total AS GrandTotal,
                   q.currency_code AS CurrencyCode,
                   q.branch_id AS BranchId, q.notes,
                   q.is_deleted AS IsDeleted,
                   q.created_at AS CreatedAt, q.created_by AS CreatedBy,
                   q.updated_at AS UpdatedAt, q.updated_by AS UpdatedBy
            FROM quotes q
            LEFT JOIN leads l ON l.id = q.lead_id
            WHERE q.id = @id AND q.is_deleted = FALSE", new { id });

        if (quote is null) return null;

        var items = await conn.QueryAsync<QuoteItem>(@"
            SELECT id, quote_id AS QuoteId, day_no AS DayNo,
                   item_date AS ItemDate, item_type AS ItemType,
                   reference_id AS ReferenceId, description,
                   qty, unit_cost AS UnitCost, unit_sale AS UnitSale,
                   tax_rate AS TaxRate, line_total AS LineTotal, sort_order AS SortOrder
            FROM quote_items
            WHERE quote_id = @id
            ORDER BY sort_order, id", new { id });

        var terms = await conn.QueryAsync<QuoteTerm>(@"
            SELECT id, quote_id AS QuoteId, terms_text AS TermsText, sort_order AS SortOrder
            FROM quote_terms
            WHERE quote_id = @id
            ORDER BY sort_order", new { id });

        quote.Items = items.AsList();
        quote.Terms = terms.AsList();
        return quote;
    }

    // ---------------- INSERT ----------------
    public async Task<Guid> InsertAsync(Quote q)
    {
        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var id = await conn.ExecuteScalarAsync<Guid>(@"
                INSERT INTO quotes (
                    quote_no, lead_id, version, valid_till, status,
                    total_cost, total_sale, total_tax, grand_total,
                    currency_code, branch_id, customer_name, notes,
                    created_at, created_by
                ) VALUES (
                    @QuoteNo, @LeadId, @Version, @ValidTill, @Status,
                    @TotalCost, @TotalSale, @TotalTax, @GrandTotal,
                    @CurrencyCode, @BranchId, @CustomerName, @Notes,
                    NOW(), @CreatedBy
                ) RETURNING id;", q, tx);

            await InsertItemsAsync(conn, tx, id, q.Items);
            await InsertTermsAsync(conn, tx, id, q.Terms);

            tx.Commit();
            return id;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ---------------- UPDATE ----------------
    public async Task UpdateAsync(Quote q)
    {
        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(@"
                UPDATE quotes SET
                    lead_id = @LeadId, valid_till = @ValidTill, status = @Status,
                    total_cost = @TotalCost, total_sale = @TotalSale,
                    total_tax = @TotalTax, grand_total = @GrandTotal,
                    currency_code = @CurrencyCode, branch_id = @BranchId,
                    customer_name = @CustomerName, notes = @Notes,
                    updated_at = NOW(), updated_by = @UpdatedBy
                WHERE id = @Id", q, tx);

            await conn.ExecuteAsync("DELETE FROM quote_items WHERE quote_id = @Id", new { q.Id }, tx);
            await conn.ExecuteAsync("DELETE FROM quote_terms WHERE quote_id = @Id", new { q.Id }, tx);

            await InsertItemsAsync(conn, tx, q.Id, q.Items);
            await InsertTermsAsync(conn, tx, q.Id, q.Terms);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ---------------- SOFT DELETE ----------------
    public async Task SoftDeleteAsync(Guid id, Guid? deletedBy)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(@"
            UPDATE quotes
            SET is_deleted = TRUE, deleted_at = NOW(),
                updated_at = NOW(), updated_by = @deletedBy
            WHERE id = @id", new { id, deletedBy });
    }

    // ---------------- STATUS ----------------
    public async Task UpdateStatusAsync(Guid id, string status, Guid? updatedBy)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(@"
            UPDATE quotes
            SET status = @status, updated_at = NOW(), updated_by = @updatedBy
            WHERE id = @id", new { id, status, updatedBy });
    }

    // ---------------- helpers ----------------
    private static async System.Threading.Tasks.Task InsertItemsAsync(
        System.Data.IDbConnection conn,
        System.Data.IDbTransaction tx,
        Guid quoteId,
        IEnumerable<QuoteItem> items)
    {
        var i = 0;
        foreach (var item in items)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO quote_items
                    (quote_id, day_no, item_date, item_type, description,
                     qty, unit_cost, unit_sale, tax_rate, line_total, sort_order)
                VALUES
                    (@QuoteId, @DayNo, @ItemDate, @ItemType, @Description,
                     @Qty, @UnitCost, @UnitSale, @TaxRate, @LineTotal, @SortOrder)",
                new
                {
                    QuoteId = quoteId,
                    item.DayNo, item.ItemDate, item.ItemType, item.Description,
                    item.Qty, item.UnitCost, item.UnitSale, item.TaxRate, item.LineTotal,
                    SortOrder = i++
                }, tx);
        }
    }

    private static async System.Threading.Tasks.Task InsertTermsAsync(
        System.Data.IDbConnection conn,
        System.Data.IDbTransaction tx,
        Guid quoteId,
        IEnumerable<QuoteTerm> terms)
    {
        var i = 0;
        foreach (var term in terms)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO quote_terms (quote_id, terms_text, sort_order)
                VALUES (@QuoteId, @TermsText, @SortOrder)",
                new { QuoteId = quoteId, term.TermsText, SortOrder = i++ }, tx);
        }
    }
}
