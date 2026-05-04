using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public interface IPaymentRepository
{
    Task<PagedResult<Payment>> ListAsync(PaymentFilter filter);
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Guid> InsertAsync(Payment payment);
    Task SoftDeleteAsync(Guid id, Guid? deletedBy);
}

public sealed class PaymentFilter
{
    public string? Search { get; set; }
    public string? Mode { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly ITenantConnectionFactory _factory;
    public PaymentRepository(ITenantConnectionFactory factory) => _factory = factory;

    public async Task<PagedResult<Payment>> ListAsync(PaymentFilter filter)
    {
        using var conn = _factory.Open();
        const string where = @"
            WHERE p.is_deleted = FALSE
              AND (@Search IS NULL OR p.payment_no ILIKE '%'||@Search||'%' OR p.reference_no ILIKE '%'||@Search||'%')
              AND (@Mode IS NULL OR p.mode = @Mode)";

        var sql = $@"
            SELECT p.id, p.payment_no AS PaymentNo, p.payment_date AS PaymentDate,
                   p.mode, p.reference_no AS ReferenceNo, p.amount,
                   p.currency_code AS CurrencyCode, p.notes, p.received_by AS ReceivedBy,
                   u.full_name AS ReceivedByName, p.is_deleted AS IsDeleted,
                   p.created_at AS CreatedAt, p.created_by AS CreatedBy
            FROM payments p
            LEFT JOIN users u ON u.id = p.received_by
            {where} ORDER BY p.payment_date DESC, p.created_at DESC LIMIT @PageSize OFFSET @Offset;
            SELECT COUNT(*) FROM payments p {where};";

        var args = new { filter.Search, filter.Mode, filter.PageSize, Offset = (filter.Page - 1) * filter.PageSize };
        using var multi = await conn.QueryMultipleAsync(sql, args);
        var items = (await multi.ReadAsync<Payment>()).AsList();
        var total = await multi.ReadFirstAsync<int>();
        return new PagedResult<Payment> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleOrDefaultAsync<Payment>(@"
            SELECT p.id, p.payment_no AS PaymentNo, p.payment_date AS PaymentDate,
                   p.mode, p.reference_no AS ReferenceNo, p.amount,
                   p.currency_code AS CurrencyCode, p.notes, p.received_by AS ReceivedBy,
                   u.full_name AS ReceivedByName, p.is_deleted AS IsDeleted,
                   p.created_at AS CreatedAt, p.created_by AS CreatedBy
            FROM payments p LEFT JOIN users u ON u.id = p.received_by
            WHERE p.id = @id AND p.is_deleted = FALSE", new { id });
    }

    public async Task<Guid> InsertAsync(Payment p)
    {
        using var conn = _factory.Open();
        return await conn.ExecuteScalarAsync<Guid>(@"
            INSERT INTO payments (payment_no, payment_date, mode, reference_no, amount,
                currency_code, notes, received_by, created_at, created_by)
            VALUES (@PaymentNo, @PaymentDate, @Mode, @ReferenceNo, @Amount,
                @CurrencyCode, @Notes, @ReceivedBy, NOW(), @CreatedBy)
            RETURNING id;", p);
    }

    public async Task SoftDeleteAsync(Guid id, Guid? deletedBy)
    {
        using var conn = _factory.Open();
        await conn.ExecuteAsync(
            "UPDATE payments SET is_deleted=TRUE, deleted_at=NOW() WHERE id=@id", new { id });
    }
}
