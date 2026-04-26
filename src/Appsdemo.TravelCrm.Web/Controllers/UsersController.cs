using Appsdemo.TravelCrm.Core.Security;
using Appsdemo.TravelCrm.Data.Repositories.Tenant;
using Appsdemo.TravelCrm.Web.Authorization;
using Appsdemo.TravelCrm.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers;

[Authorize, Route("users")]
public sealed class UsersController : Controller
{
    private readonly IUserRepository _users;
    public UsersController(IUserRepository users) => _users = users;

    [HttpGet("")]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 25)
    {
        var items = await _users.ListAsync(search, page, pageSize);
        var total = await _users.CountAsync(search);
        return View("~/Views/Users/Index.cshtml", new UserListVm
        {
            Items = items, Search = search, Page = page, PageSize = pageSize, TotalCount = total
        });
    }
}

[Authorize, Route("roles")]
public sealed class RolesController : Controller
{
    private readonly IRoleRepository _roles;
    public RolesController(IRoleRepository roles) => _roles = roles;

    [HttpGet("")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> Index()
    {
        var items = await _roles.ListAsync();
        return View("~/Views/Roles/Index.cshtml", new RoleListVm { Items = items });
    }
}
