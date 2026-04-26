namespace Appsdemo.TravelCrm.Core.Models.Tenant;

public sealed class Lead
{
    public Guid Id { get; set; }
    public string LeadNo { get; set; } = "";
    public string? Source { get; set; }
    public string CustomerName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? DestinationId { get; set; }
    public DateTime? TravelDate { get; set; }
    public int? TravelNights { get; set; }
    public int PaxAdults { get; set; }
    public int PaxChildren { get; set; }
    public decimal? Budget { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string Status { get; set; } = LeadStatus.New;
    public string? LostReason { get; set; }
    public Guid? AssignedTo { get; set; }
    public Guid? BranchId { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public sealed class LeadListItem
{
    public Guid Id { get; set; }
    public string LeadNo { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "";
    public string? AssignedToName { get; set; }
    public string? BranchName { get; set; }
    public DateTime? TravelDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class LeadFollowup
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public DateTimeOffset FollowupDate { get; set; }
    public string? Mode { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? NextFollowupDate { get; set; }
    public Guid? DoneBy { get; set; }
    public string? DoneByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public static class LeadStatus
{
    public const string New        = "New";
    public const string Contacted  = "Contacted";
    public const string Qualified  = "Qualified";
    public const string Quoted     = "Quoted";
    public const string Won        = "Won";
    public const string Lost       = "Lost";

    public static readonly string[] All =
        { New, Contacted, Qualified, Quoted, Won, Lost };

    public static string BadgeColor(string status) => status switch
    {
        New        => "azure",
        Contacted  => "indigo",
        Qualified  => "purple",
        Quoted     => "yellow",
        Won        => "green",
        Lost       => "red",
        _          => "secondary"
    };
}

public static class LeadSource
{
    public static readonly string[] All =
        { "Website", "Walk-in", "Referral", "Phone", "Email", "Social", "Other" };
}

public static class FollowupMode
{
    public static readonly string[] All =
        { "Phone", "Email", "Meeting", "WhatsApp", "Other" };
}
