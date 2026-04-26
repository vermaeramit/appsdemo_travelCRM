using Appsdemo.TravelCrm.Core.Multitenancy;
using Appsdemo.TravelCrm.Core.Security;
using Appsdemo.TravelCrm.Web.Authorization;
using Appsdemo.TravelCrm.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly ITenantContextAccessor _tenant;

    public DashboardController(ITenantContextAccessor tenant) => _tenant = tenant;

    [HttpGet("/")]
    [HasPermission(Permissions.Dashboard.View)]
    public IActionResult Index()
    {
        var t = _tenant.Current;
        return View("~/Views/Dashboard/Index.cshtml", new DashboardVm
        {
            CompanyName = t?.CompanyName ?? "",
            PlanName = t?.PlanName ?? "",
            LeadsThisMonth = 0,
            QuotesThisMonth = 0,
            BookingsThisMonth = 0,
            RevenueThisMonth = 0m
        });
    }
}
