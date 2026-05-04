using Appsdemo.TravelCrm.Core.Security;
using Appsdemo.TravelCrm.Data.Repositories.Tenant;
using Appsdemo.TravelCrm.Web.Authorization;
using Appsdemo.TravelCrm.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers;

[Authorize, Route("audit")]
public sealed class AuditController : Controller
{
    private readonly IAuditRepository _audit;
    public AuditController(IAuditRepository audit) => _audit = audit;

    [HttpGet(""), HasPermission(Permissions.Audit.View)]
    public async Task<IActionResult> Index(string? search, string? entity, int page = 1, int pageSize = 50)
    {
        var result = await _audit.ListAsync(new AuditFilter { Search = search, Entity = entity, Page = page, PageSize = pageSize });
        return View("~/Views/Audit/Index.cshtml", new AuditIndexVm
        {
            Search = search, Entity = entity, Page = page, PageSize = pageSize, Result = result
        });
    }
}
