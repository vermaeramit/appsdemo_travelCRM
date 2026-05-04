using System.ComponentModel.DataAnnotations;
using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;

namespace Appsdemo.TravelCrm.Web.Models.ViewModels;

public sealed class PaymentIndexVm
{
    public string? Search { get; set; }
    public string? Mode { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public PagedResult<Payment> Result { get; set; } = new();
}

public sealed class PaymentFormVm
{
    [Required]
    public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public string Mode { get; set; } = PaymentMode.Cash;

    [MaxLength(100)]
    public string? ReferenceNo { get; set; }

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string CurrencyCode { get; set; } = "INR";

    [MaxLength(500)]
    public string? Notes { get; set; }
}
