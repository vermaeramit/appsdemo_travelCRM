namespace Appsdemo.TravelCrm.Core.Models.Tenant;

public sealed class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNo { get; set; } = "";
    public Guid? BookingId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerGstin { get; set; }
    public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string Status { get; set; } = InvoiceStatus.Draft;
    public Guid? BranchId { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public List<InvoiceItem> Items { get; set; } = new();
}

public sealed class InvoiceItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = "";
    public string? HsnSac { get; set; }
    public decimal Qty { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
}

public sealed class InvoiceListItem
{
    public Guid Id { get; set; }
    public string InvoiceNo { get; set; } = "";
    public Guid? BookingId { get; set; }
    public string? BookingNo { get; set; }
    public string CustomerName { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal GrandTotal { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string Status { get; set; } = "";
    public string? BranchName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public static class InvoiceStatus
{
    public const string Draft     = "Draft";
    public const string Sent      = "Sent";
    public const string Paid      = "Paid";
    public const string Overdue   = "Overdue";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Draft, Sent, Paid, Overdue, Cancelled];

    public static string BadgeColor(string? status) => status switch
    {
        Draft     => "secondary",
        Sent      => "azure",
        Paid      => "success",
        Overdue   => "danger",
        Cancelled => "danger",
        _         => "secondary"
    };
}
