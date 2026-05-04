using Appsdemo.TravelCrm.Core.Multitenancy;
using Appsdemo.TravelCrm.Core.Security;
using Appsdemo.TravelCrm.Data.Repositories.Tenant;
using Appsdemo.TravelCrm.Data.Services;
using Appsdemo.TravelCrm.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers;

[Authorize, Route("settings")]
public sealed class SettingsController : Controller
{
    private readonly ITenantContextAccessor _tenant;
    private readonly INumberSequenceService _numbers;

    public SettingsController(ITenantContextAccessor tenant, INumberSequenceService numbers)
    {
        _tenant = tenant; _numbers = numbers;
    }

    [HttpGet(""), HasPermission(Permissions.Settings.View)]
    public IActionResult Index()
    {
        var t = _tenant.Current;
        ViewData["Title"] = "Settings";
        ViewData["Tenant"] = t;
        return View("~/Views/Settings/Index.cshtml", t);
    }
}
