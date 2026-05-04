using Appsdemo.TravelCrm.Core.Multitenancy;
using Appsdemo.TravelCrm.Core.Security;
using Appsdemo.TravelCrm.Data.Repositories.Tenant;
using Appsdemo.TravelCrm.Web.Authorization;
using Appsdemo.TravelCrm.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly ITenantContextAccessor _tenant;
    private readonly IDashboardRepository _dash;

    public DashboardController(ITenantContextAccessor tenant, IDashboardRepository dash)
    {
        _tenant = tenant; _dash = dash;
    }

    [HttpGet("/")]
    [HasPermission(Permissions.Dashboard.View)]
    public async Task<IActionResult> Index()
    {
        var t = _tenant.Current;
        var stats = await _dash.GetStatsAsync();
        return View("~/Views/Dashboard/Index.cshtml", new DashboardVm
        {
            CompanyName      = t?.CompanyName ?? "",
            PlanName         = t?.PlanName ?? "",
            LeadsThisMonth   = stats.LeadsThisMonth,
            QuotesThisMonth  = stats.QuotesThisMonth,
            BookingsThisMonth = stats.BookingsThisMonth,
            RevenueThisMonth = stats.RevenueThisMonth,
            ActiveLeads      = stats.ActiveLeads,
            OpenQuotes       = stats.OpenQuotes,
            ConfirmedBookings = stats.ConfirmedBookings,
            TotalRevenue     = stats.TotalRevenue
        });
    }
}
