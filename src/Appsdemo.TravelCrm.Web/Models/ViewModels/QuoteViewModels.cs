using System.ComponentModel.DataAnnotations;
using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;

namespace Appsdemo.TravelCrm.Web.Models.ViewModels;

public sealed class QuoteIndexVm
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public PagedResult<QuoteListItem> Result { get; set; } = new();
}

public sealed class QuoteFormVm
{
    public Guid? Id { get; set; }
    public string? QuoteNo { get; set; }

    public Guid? LeadId { get; set; }
    public string? CustomerNameDisplay { get; set; }

    public DateOnly? ValidTill { get; set; }
    public string Status { get; set; } = QuoteStatus.Draft;

    [Required, MaxLength(3)]
    public string CurrencyCode { get; set; } = "INR";

    public Guid? BranchId { get; set; }
    public string? Notes { get; set; }

    public List<QuoteItemVm> Items { get; set; } = new();
    public List<string> Terms { get; set; } = new();

    // Dropdowns
    public List<DropdownItem> Branches { get; set; } = new();
    public List<DropdownItem> Leads { get; set; } = new();
}

public sealed class QuoteItemVm
{
    public int? DayNo { get; set; }
    public DateOnly? ItemDate { get; set; }
    public string ItemType { get; set; } = QuoteItemType.Misc;
    public string? Description { get; set; }
    public decimal Qty { get; set; } = 1;
    public decimal UnitCost { get; set; }
    public decimal UnitSale { get; set; }
    public decimal TaxRate { get; set; }
    public int SortOrder { get; set; }
}

public sealed class QuoteDetailsVm
{
    public Quote Quote { get; set; } = null!;
    public string? LeadNo { get; set; }
    public string? BranchName { get; set; }
    public bool CanEdit { get; set; }
    public bool CanApprove { get; set; }
    public bool CanSend { get; set; }
    public bool CanDelete { get; set; }
}
