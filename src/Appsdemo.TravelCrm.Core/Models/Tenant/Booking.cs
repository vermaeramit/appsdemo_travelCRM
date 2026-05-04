namespace Appsdemo.TravelCrm.Core.Models.Tenant;

public sealed class Booking
{
    public Guid Id { get; set; }
    public string BookingNo { get; set; } = "";
    public Guid? QuoteId { get; set; }
    public Guid? LeadId { get; set; }
    public string CustomerName { get; set; } = "";
    public DateOnly? TravelStart { get; set; }
    public DateOnly? TravelEnd { get; set; }
    public string Status { get; set; } = BookingStatus.Confirmed;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public Guid? BranchId { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public List<BookingItem> Items { get; set; } = new();
}

public sealed class BookingItem
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
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

public sealed class BookingListItem
{
    public Guid Id { get; set; }
    public string BookingNo { get; set; } = "";
    public Guid? QuoteId { get; set; }
    public string? QuoteNo { get; set; }
    public string CustomerName { get; set; } = "";
    public DateOnly? TravelStart { get; set; }
    public DateOnly? TravelEnd { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string? BranchName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public static class BookingStatus
{
    public const string Confirmed  = "Confirmed";
    public const string InProgress = "In Progress";
    public const string Completed  = "Completed";
    public const string Cancelled  = "Cancelled";

    public static readonly string[] All = [Confirmed, InProgress, Completed, Cancelled];

    public static string BadgeColor(string? status) => status switch
    {
        Confirmed  => "success",
        InProgress => "azure",
        Completed  => "teal",
        Cancelled  => "danger",
        _          => "secondary"
    };
}
