using System.ComponentModel.DataAnnotations;
using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;

namespace Appsdemo.TravelCrm.Web.Models.ViewModels;

public sealed class InvoiceIndexVm
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public PagedResult<InvoiceListItem> Result { get; set; } = new();
}

public sealed class InvoiceFormVm
{
    public Guid? Id { get; set; }
    public string? InvoiceNo { get; set; }

    public Guid? BookingId { get; set; }
    public string? BookingNo { get; set; }

    [Required, MaxLength(200)]
    public string CustomerName { get; set; } = "";

    [MaxLength(20)]
    public string? CustomerGstin { get; set; }

    public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? DueDate { get; set; }

    public string Status { get; set; } = InvoiceStatus.Draft;

    [Required, MaxLength(3)]
    public string CurrencyCode { get; set; } = "INR";

    public Guid? BranchId { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceItemVm> Items { get; set; } = new();

    // Dropdowns
    public List<DropdownItem> Branches { get; set; } = new();
    public List<DropdownItem> Bookings { get; set; } = new();
}

public sealed class InvoiceItemVm
{
    [MaxLength(500)]
    public string Description { get; set; } = "";
    [MaxLength(20)]
    public string? HsnSac { get; set; }
    public decimal Qty { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public int SortOrder { get; set; }
}

public sealed class InvoiceDetailsVm
{
    public Invoice Invoice { get; set; } = null!;
    public string? BookingNo { get; set; }
    public string? BranchName { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
