using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public interface IDashboardRepository
{
    Task<DashboardStats> GetStatsAsync();
}

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly ITenantConnectionFactory _factory;
    public DashboardRepository(ITenantConnectionFactory factory) => _factory = factory;

    public async Task<DashboardStats> GetStatsAsync()
    {
        using var conn = _factory.Open();
        return await conn.QuerySingleAsync<DashboardStats>(@"
            SELECT
                (SELECT COUNT(*) FROM leads
                 WHERE is_deleted=FALSE
                   AND DATE_TRUNC('month', created_at) = DATE_TRUNC('month', NOW()))
                    AS LeadsThisMonth,
                (SELECT COUNT(*) FROM quotes
                 WHERE is_deleted=FALSE
                   AND DATE_TRUNC('month', created_at) = DATE_TRUNC('month', NOW()))
                    AS QuotesThisMonth,
                (SELECT COUNT(*) FROM bookings
                 WHERE is_deleted=FALSE
                   AND DATE_TRUNC('month', created_at) = DATE_TRUNC('month', NOW()))
                    AS BookingsThisMonth,
                (SELECT COALESCE(SUM(grand_total), 0) FROM invoices
                 WHERE is_deleted=FALSE AND status='Paid'
                   AND DATE_TRUNC('month', invoice_date::timestamptz) = DATE_TRUNC('month', NOW()))
                    AS RevenueThisMonth,
                (SELECT COUNT(*) FROM leads
                 WHERE is_deleted=FALSE AND status NOT IN ('Won','Lost'))
                    AS ActiveLeads,
                (SELECT COUNT(*) FROM quotes
                 WHERE is_deleted=FALSE AND status IN ('Draft','Sent'))
                    AS OpenQuotes,
                (SELECT COUNT(*) FROM bookings
                 WHERE is_deleted=FALSE AND status IN ('Confirmed','In Progress'))
                    AS ConfirmedBookings,
                (SELECT COALESCE(SUM(grand_total), 0) FROM invoices
                 WHERE is_deleted=FALSE AND status='Paid')
                    AS TotalRevenue");
    }
}
