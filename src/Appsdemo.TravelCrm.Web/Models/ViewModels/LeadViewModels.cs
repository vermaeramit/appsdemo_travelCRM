using System.ComponentModel.DataAnnotations;
using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;

namespace Appsdemo.TravelCrm.Web.Models.ViewModels;

public sealed class LeadIndexVm
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public Guid? AssignedTo { get; set; }
    public Guid? BranchId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public PagedResult<LeadListItem> Result { get; set; } = new();
    public IReadOnlyList<DropdownItem> Users { get; set; } = Array.Empty<DropdownItem>();
    public IReadOnlyList<DropdownItem> Branches { get; set; } = Array.Empty<DropdownItem>();
}

public sealed class LeadFormVm
{
    public Guid? Id { get; set; }
    public string? LeadNo { get; set; }

    [Required, StringLength(200)] public string CustomerName { get; set; } = "";

    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [StringLength(30)] public string? Phone { get; set; }

    public string? Source { get; set; }

    [DataType(DataType.Date)] public DateTime? TravelDate { get; set; }
    [Range(0, 365)] public int? TravelNights { get; set; }
    [Range(0, 999)] public int PaxAdults { get; set; }
    [Range(0, 999)] public int PaxChildren { get; set; }
    [Range(0, 99999999)] public decimal? Budget { get; set; }
    public string CurrencyCode { get; set; } = "INR";

    public string Status { get; set; } = LeadStatus.New;
    public string? LostReason { get; set; }

    public Guid? AssignedTo { get; set; }
    public Guid? BranchId { get; set; }

    public string? Notes { get; set; }

    public IReadOnlyList<DropdownItem> Users { get; set; } = Array.Empty<DropdownItem>();
    public IReadOnlyList<DropdownItem> Branches { get; set; } = Array.Empty<DropdownItem>();
}

public sealed class LeadDetailsVm
{
    public Lead Lead { get; set; } = new();
    public string? AssignedToName { get; set; }
    public string? BranchName { get; set; }
    public IReadOnlyList<LeadFollowup> Followups { get; set; } = Array.Empty<LeadFollowup>();
    public NewFollowupVm NewFollowup { get; set; } = new();
}

public sealed class NewFollowupVm
{
    public Guid LeadId { get; set; }

    [Required, DataType(DataType.DateTime)]
    public DateTime FollowupDate { get; set; } = DateTime.Now;

    [Required] public string Mode { get; set; } = "Phone";

    [Required, StringLength(2000)] public string Notes { get; set; } = "";

    [DataType(DataType.DateTime)]
    public DateTime? NextFollowupDate { get; set; }
}

public sealed class DropdownItem
{
    public string Value { get; set; } = "";
    public string Text { get; set; } = "";
}
