using System.ComponentModel.DataAnnotations;
using Appsdemo.TravelCrm.Core.Models.Tenant;

namespace Appsdemo.TravelCrm.Web.Models.ViewModels;

public sealed class BranchListVm
{
    public IReadOnlyList<Branch> Items { get; set; } = Array.Empty<Branch>();
}

public sealed class BranchFormVm
{
    public Guid? Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Required, MaxLength(30)]
    public string Code { get; set; } = "";

    [MaxLength(300)] public string? Address { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(100)] public string? State { get; set; }
    [MaxLength(100)] public string? Country { get; set; } = "India";
    [MaxLength(30)]  public string? Phone { get; set; }
    [EmailAddress, MaxLength(150)] public string? Email { get; set; }
    [MaxLength(20)]  public string? Gstin { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsHeadOffice { get; set; }
}
