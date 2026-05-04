namespace Appsdemo.TravelCrm.Core.Models.Tenant;

public sealed class Quote
{
    public Guid Id { get; set; }
    public string QuoteNo { get; set; } = "";
    public Guid? LeadId { get; set; }
    public string? CustomerName { get; set; }
    public int Version { get; set; } = 1;
    public DateOnly? ValidTill { get; set; }
    public string Status { get; set; } = QuoteStatus.Draft;
    public decimal TotalCost { get; set; }
    public decimal TotalSale { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public Guid? BranchId { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public List<QuoteItem> Items { get; set; } = new();
    public List<QuoteTerm> Terms { get; set; } = new();
}

public sealed class QuoteItem
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public int? DayNo { get; set; }
    public DateOnly? ItemDate { get; set; }
    public string? ItemType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Description { get; set; }
    public decimal Qty { get; set; } = 1;
    public decimal UnitCost { get; set; }
    public decimal UnitSale { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
}

public sealed class QuoteTerm
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public string TermsText { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class QuoteListItem
{
    public Guid Id { get; set; }
    public string QuoteNo { get; set; } = "";
    public Guid? LeadId { get; set; }
    public string? LeadNo { get; set; }
    public string? CustomerName { get; set; }
    public int Version { get; set; }
    public DateOnly? ValidTill { get; set; }
    public string Status { get; set; } = "";
    public decimal GrandTotal { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string? BranchName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public static class QuoteStatus
{
    public const string Draft    = "Draft";
    public const string Sent     = "Sent";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Expired  = "Expired";
    public const string Won      = "Won";
    public const string Lost     = "Lost";

    public static readonly string[] All = [Draft, Sent, Approved, Rejected, Expired, Won, Lost];

    public static string BadgeColor(string? status) => status switch
    {
        Draft    => "secondary",
        Sent     => "azure",
        Approved => "teal",
        Won      => "success",
        Rejected => "danger",
        Lost     => "danger",
        Expired  => "warning",
        _        => "secondary"
    };
}

public static class QuoteItemType
{
    public const string Hotel       = "Hotel";
    public const string Flight      = "Flight";
    public const string Transport   = "Transport";
    public const string Sightseeing = "Sightseeing";
    public const string Meal        = "Meal";
    public const string Misc        = "Misc";

    public static readonly string[] All = [Hotel, Flight, Transport, Sightseeing, Meal, Misc];
}
