using System.ComponentModel.DataAnnotations;
using Appsdemo.TravelCrm.Core.Models.Tenant;

namespace Appsdemo.TravelCrm.Web.Models.ViewModels;

public sealed class UserFormVm
{
    public Guid? Id { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    [MaxLength(50)]
    public string? Username { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    public Guid? BranchId { get; set; }

    [MinLength(8), DataType(DataType.Password)]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;

    public List<Guid> SelectedRoleIds { get; set; } = new();

    // Dropdowns
    public List<DropdownItem> Branches { get; set; } = new();
    public List<Role> AllRoles { get; set; } = new();
}

public sealed class UserDetailsVm
{
    public User User { get; set; } = null!;
    public IReadOnlyList<Role> Roles { get; set; } = Array.Empty<Role>();
    public string? BranchName { get; set; }
}

public sealed class ResetPasswordVm
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";

    [Required, MinLength(8), DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";
}
