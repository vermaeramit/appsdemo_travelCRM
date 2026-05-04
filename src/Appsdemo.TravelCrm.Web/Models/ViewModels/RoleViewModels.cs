using System.ComponentModel.DataAnnotations;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Core.Security;

namespace Appsdemo.TravelCrm.Web.Models.ViewModels;

public sealed class RoleDetailsVm
{
    public Role Role { get; set; } = null!;
    public IReadOnlyList<string> PermissionKeys { get; set; } = Array.Empty<string>();
    public IEnumerable<IGrouping<string, (string Key, string Module, string Action, string Display)>> GroupedPermissions { get; set; }
        = Array.Empty<IGrouping<string, (string, string, string, string)>>();
}

public sealed class RoleFormVm
{
    public Guid? Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(300)]
    public string? Description { get; set; }

    public List<string> SelectedPermissions { get; set; } = new();
    public IEnumerable<IGrouping<string, (string Key, string Module, string Action, string Display)>> AllPermissions { get; set; }
        = Array.Empty<IGrouping<string, (string, string, string, string)>>();
}
