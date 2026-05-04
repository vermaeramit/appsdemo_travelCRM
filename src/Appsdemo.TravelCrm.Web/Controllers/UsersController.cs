using System.Security.Claims;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Core.Security;
using Appsdemo.TravelCrm.Data.Repositories.Tenant;
using Appsdemo.TravelCrm.Web.Authorization;
using Appsdemo.TravelCrm.Web.Models.ViewModels;
using Appsdemo.TravelCrm.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers;

[Authorize, Route("users")]
public sealed class UsersController : Controller
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IBranchRepository _branches;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogger _audit;

    public UsersController(IUserRepository users, IRoleRepository roles,
        IBranchRepository branches, IPasswordHasher hasher, IAuditLogger audit)
    {
        _users = users; _roles = roles; _branches = branches;
        _hasher = hasher; _audit = audit;
    }

    [HttpGet(""), HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 25)
    {
        var items = await _users.ListAsync(search, page, pageSize);
        var total = await _users.CountAsync(search);
        return View("~/Views/Users/Index.cshtml", new UserListVm
        { Items = items, Search = search, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("new"), HasPermission(Permissions.Users.Create)]
    public async Task<IActionResult> Create()
    {
        var vm = new UserFormVm { IsActive = true, MustChangePassword = true };
        await PopulateDropdownsAsync(vm);
        return View("~/Views/Users/Form.cshtml", vm);
    }

    [HttpPost("new"), ValidateAntiForgeryToken, HasPermission(Permissions.Users.Create)]
    public async Task<IActionResult> Create(UserFormVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Password))
            ModelState.AddModelError(nameof(vm.Password), "Password is required for new users.");

        if (!ModelState.IsValid) { await PopulateDropdownsAsync(vm); return View("~/Views/Users/Form.cshtml", vm); }

        var user = new User
        {
            Email = vm.Email, Username = string.IsNullOrWhiteSpace(vm.Username) ? null : vm.Username,
            FullName = vm.FullName, Phone = vm.Phone, BranchId = vm.BranchId,
            IsActive = vm.IsActive, MustChangePassword = vm.MustChangePassword,
            PasswordHash = _hasher.Hash(vm.Password!), CreatedBy = CurrentUserId
        };

        var newId = await _users.InsertAsync(user);
        if (vm.SelectedRoleIds.Count > 0)
            await _users.SetRolesAsync(newId, vm.SelectedRoleIds);

        await _audit.WriteAsync("user.create", "users", newId.ToString(), new { user.Email, user.FullName });
        TempData["Success"] = $"User {user.FullName} created.";
        return RedirectToAction(nameof(Details), new { id = newId });
    }

    [HttpGet("{id:guid}"), HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null) return NotFound();
        var roles = await _users.GetRolesAsync(id);
        string? branchName = null;
        if (user.BranchId.HasValue)
            branchName = (await _branches.GetByIdAsync(user.BranchId.Value))?.Name;
        return View("~/Views/Users/Details.cshtml", new UserDetailsVm
        { User = user, Roles = roles, BranchName = branchName });
    }

    [HttpGet("{id:guid}/edit"), HasPermission(Permissions.Users.Edit)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null) return NotFound();
        var userRoles = await _users.GetRolesAsync(id);
        var vm = new UserFormVm
        {
            Id = user.Id, FullName = user.FullName, Email = user.Email,
            Username = user.Username, Phone = user.Phone, BranchId = user.BranchId,
            IsActive = user.IsActive, MustChangePassword = user.MustChangePassword,
            SelectedRoleIds = userRoles.Select(r => r.Id).ToList()
        };
        await PopulateDropdownsAsync(vm);
        return View("~/Views/Users/Form.cshtml", vm);
    }

    [HttpPost("{id:guid}/edit"), ValidateAntiForgeryToken, HasPermission(Permissions.Users.Edit)]
    public async Task<IActionResult> Edit(Guid id, UserFormVm vm)
    {
        ModelState.Remove(nameof(vm.Password));
        if (!ModelState.IsValid) { await PopulateDropdownsAsync(vm); return View("~/Views/Users/Form.cshtml", vm); }

        var user = await _users.GetByIdAsync(id);
        if (user is null) return NotFound();

        user.FullName = vm.FullName; user.Phone = vm.Phone; user.BranchId = vm.BranchId;
        user.IsActive = vm.IsActive; user.MustChangePassword = vm.MustChangePassword;
        user.UpdatedBy = CurrentUserId;

        await _users.UpdateAsync(user);
        await _users.SetRolesAsync(id, vm.SelectedRoleIds);
        await _audit.WriteAsync("user.update", "users", id.ToString(), new { user.Email });
        TempData["Success"] = $"{user.FullName} updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("{id:guid}/reset-password"), HasPermission(Permissions.Users.ResetPassword)]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null) return NotFound();
        return View("~/Views/Users/ResetPassword.cshtml",
            new ResetPasswordVm { UserId = id, UserName = user.FullName });
    }

    [HttpPost("{id:guid}/reset-password"), ValidateAntiForgeryToken, HasPermission(Permissions.Users.ResetPassword)]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordVm vm)
    {
        if (!ModelState.IsValid) return View("~/Views/Users/ResetPassword.cshtml", vm);
        await _users.UpdatePasswordAsync(id, _hasher.Hash(vm.NewPassword), CurrentUserId);
        await _audit.WriteAsync("user.password_reset", "users", id.ToString(), null);
        TempData["Success"] = "Password reset. User will be prompted to change it on next login.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/toggle-active"), ValidateAntiForgeryToken, HasPermission(Permissions.Users.Edit)]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null) return NotFound();
        await _users.SetActiveAsync(id, !user.IsActive, CurrentUserId);
        await _audit.WriteAsync("user.toggle_active", "users", id.ToString(), new { NewState = !user.IsActive });
        TempData["Success"] = user.IsActive ? $"{user.FullName} deactivated." : $"{user.FullName} activated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;

    private async Task PopulateDropdownsAsync(UserFormVm vm)
    {
        vm.AllRoles = (await _roles.ListAsync()).ToList();
        var branches = await _branches.ListActiveAsync();
        vm.Branches = branches.Select(b => new DropdownItem { Value = b.Id.ToString(), Text = b.Name }).ToList();
    }
}

[Authorize, Route("roles")]
public sealed class RolesController : Controller
{
    private readonly IRoleRepository _roles;
    private readonly IAuditLogger _audit;

    public RolesController(IRoleRepository roles, IAuditLogger audit)
    {
        _roles = roles; _audit = audit;
    }

    [HttpGet(""), HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> Index()
    {
        var items = await _roles.ListAsync();
        return View("~/Views/Roles/Index.cshtml", new RoleListVm { Items = items });
    }

    [HttpGet("{id:guid}"), HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var role = await _roles.GetByIdAsync(id);
        if (role is null) return NotFound();
        var keys = await _roles.GetPermissionsAsync(id);
        var grouped = Permissions.All().GroupBy(p => p.Module);
        return View("~/Views/Roles/Details.cshtml", new RoleDetailsVm
        {
            Role = role, PermissionKeys = keys, GroupedPermissions = grouped
        });
    }

    [HttpGet("{id:guid}/edit"), HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var role = await _roles.GetByIdAsync(id);
        if (role is null) return NotFound();
        var keys = await _roles.GetPermissionsAsync(id);
        return View("~/Views/Roles/Form.cshtml", new RoleFormVm
        {
            Id = id, Name = role.Name, Description = role.Description,
            SelectedPermissions = keys.ToList(),
            AllPermissions = Permissions.All().GroupBy(p => p.Module)
        });
    }

    [HttpPost("{id:guid}/edit"), ValidateAntiForgeryToken, HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> Edit(Guid id, RoleFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AllPermissions = Permissions.All().GroupBy(p => p.Module);
            return View("~/Views/Roles/Form.cshtml", vm);
        }
        await _roles.SetPermissionsAsync(id, vm.SelectedPermissions);
        await _audit.WriteAsync("role.update_permissions", "roles", id.ToString(), new { Count = vm.SelectedPermissions.Count });
        TempData["Success"] = "Role permissions updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("new"), HasPermission(Permissions.Roles.Manage)]
    public IActionResult Create() => View("~/Views/Roles/Form.cshtml", new RoleFormVm
    {
        AllPermissions = Permissions.All().GroupBy(p => p.Module)
    });

    [HttpPost("new"), ValidateAntiForgeryToken, HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> Create(RoleFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AllPermissions = Permissions.All().GroupBy(p => p.Module);
            return View("~/Views/Roles/Form.cshtml", vm);
        }
        var newId = await _roles.InsertAsync(new Role { Name = vm.Name, Description = vm.Description });
        await _roles.SetPermissionsAsync(newId, vm.SelectedPermissions);
        await _audit.WriteAsync("role.create", "roles", newId.ToString(), new { vm.Name });
        TempData["Success"] = $"Role '{vm.Name}' created.";
        return RedirectToAction(nameof(Details), new { id = newId });
    }
}
