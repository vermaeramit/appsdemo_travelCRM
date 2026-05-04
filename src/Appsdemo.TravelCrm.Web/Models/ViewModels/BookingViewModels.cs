using System.ComponentModel.DataAnnotations;
using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;

namespace Appsdemo.TravelCrm.Web.Models.ViewModels;

public sealed class BookingIndexVm
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public PagedResult<BookingListItem> Result { get; set; } = new();
}

public sealed class BookingFormVm
{
    public Guid? Id { get; set; }
    public string? BookingNo { get; set; }

    public Guid? QuoteId { get; set; }
    public string? QuoteNo { get; set; }

    [Required, MaxLength(200)]
    public string CustomerName { get; set; } = "";

    public DateOnly? TravelStart { get; set; }
    public DateOnly? TravelEnd { get; set; }
    public string Status { get; set; } = BookingStatus.Confirmed;

    [Required, MaxLength(3)]
    public string CurrencyCode { get; set; } = "INR";

    public Guid? BranchId { get; set; }
    public string? Notes { get; set; }
    public List<BookingItemVm> Items { get; set; } = new();

    // Dropdowns
    public List<DropdownItem> Branches { get; set; } = new();
}

public sealed class BookingItemVm
{
    public int? DayNo { get; set; }
    public DateOnly? ItemDate { get; set; }
    public string ItemType { get; set; } = "Misc";
    public string? Description { get; set; }
    public decimal Qty { get; set; } = 1;
    public decimal UnitCost { get; set; }
    public decimal UnitSale { get; set; }
    public decimal TaxRate { get; set; }
    public int SortOrder { get; set; }
}

public sealed class BookingDetailsVm
{
    public Booking Booking { get; set; } = null!;
    public string? QuoteNo { get; set; }
    public string? BranchName { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
