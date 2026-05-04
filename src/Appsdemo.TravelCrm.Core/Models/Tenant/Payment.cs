namespace Appsdemo.TravelCrm.Core.Models.Tenant;

public sealed class Payment
{
    public Guid Id { get; set; }
    public string PaymentNo { get; set; } = "";
    public DateOnly PaymentDate { get; set; }
    public string Mode { get; set; } = "";
    public string? ReferenceNo { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string? Notes { get; set; }
    public Guid? ReceivedBy { get; set; }
    public string? ReceivedByName { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}

public static class PaymentMode
{
    public const string Cash         = "Cash";
    public const string BankTransfer = "Bank Transfer";
    public const string Cheque       = "Cheque";
    public const string Upi          = "UPI";
    public const string CreditCard   = "Credit Card";
    public const string Online       = "Online";

    public static readonly string[] All = [Cash, BankTransfer, Cheque, Upi, CreditCard, Online];
}
