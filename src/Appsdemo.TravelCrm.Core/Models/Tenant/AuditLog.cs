namespace Appsdemo.TravelCrm.Core.Models.Tenant;

public sealed class AuditLogEntry
{
    public long Id { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorEmail { get; set; }
    public string Action { get; set; } = "";
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public string? Payload { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class DashboardStats
{
    public int LeadsThisMonth { get; set; }
    public int QuotesThisMonth { get; set; }
    public int BookingsThisMonth { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public int ActiveLeads { get; set; }
    public int OpenQuotes { get; set; }
    public int ConfirmedBookings { get; set; }
    public decimal TotalRevenue { get; set; }
}
