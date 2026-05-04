using Appsdemo.TravelCrm.Core.Security;
using Appsdemo.TravelCrm.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers;

public abstract class PlaceholderController : Controller
{
    protected IActionResult ComingSoon(string title, string description, string icon = "ti ti-rocket")
    {
        ViewData["Title"] = title;
        ViewData["Description"] = description;
        ViewData["Icon"] = icon;
        return View("~/Views/Shared/ComingSoon.cshtml");
    }
}

[Authorize, Route("vouchers")]
public sealed class VouchersController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Vouchers.View), RequireFeature(Features.ModuleVouchers)]
    public IActionResult Index() => ComingSoon("Vouchers",
        "Generate supplier vouchers for hotels, transport, sightseeing.", "ti ti-ticket");
}

[Authorize, Route("reports")]
public sealed class ReportsController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Reports.View), RequireFeature(Features.ModuleReports)]
    public IActionResult Index() => ComingSoon("Reports",
        "Sales, operations and financial reports.", "ti ti-chart-bar");
}

// ----- Master data sub-pages share one route prefix -----

[Authorize, Route("masters")]
public sealed class MastersController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Masters.View)]
    public IActionResult Index()
    {
        var prefix = HttpContext.Request.PathBase.Value ?? "";
        return Redirect(prefix + "/masters/destinations");
    }
}

[Authorize, Route("masters/destinations")]
public sealed class DestinationsController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Masters.View)]
    public IActionResult Index() => ComingSoon("Destinations", "Country / state / city / region master.", "ti ti-map-pin");
}

[Authorize, Route("masters/hotels")]
public sealed class HotelsController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Masters.View)]
    public IActionResult Index() => ComingSoon("Hotels", "Hotels with star rating, address, contact.", "ti ti-building");
}

[Authorize, Route("masters/room-types")]
public sealed class RoomTypesController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Masters.View)]
    public IActionResult Index() => ComingSoon("Room Types", "Per-hotel room types with occupancy and rate.", "ti ti-bed");
}

[Authorize, Route("masters/sightseeing")]
public sealed class SightseeingController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Masters.View)]
    public IActionResult Index() => ComingSoon("Sightseeing", "Tours, durations, base costs.", "ti ti-camera");
}

[Authorize, Route("masters/transport")]
public sealed class TransportController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Masters.View)]
    public IActionResult Index() => ComingSoon("Transport Services", "Vehicle types, capacity, base rates.", "ti ti-car");
}

[Authorize, Route("masters/suppliers")]
public sealed class SuppliersController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Masters.View)]
    public IActionResult Index() => ComingSoon("Suppliers", "Hotel/transport/activity/DMC suppliers.", "ti ti-truck-delivery");
}

[Authorize, Route("masters/services")]
public sealed class ServicesController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Masters.View)]
    public IActionResult Index() => ComingSoon("Services", "Generic catalogue services and rates.", "ti ti-tag");
}

[Authorize, Route("masters/tax-rates")]
public sealed class TaxRatesController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Masters.View)]
    public IActionResult Index() => ComingSoon("Tax Rates", "GST and other tax configurations.", "ti ti-percentage");
}

[Authorize, Route("masters/currencies")]
public sealed class CurrenciesController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Masters.View)]
    public IActionResult Index() => ComingSoon("Currencies", "Currency codes and exchange rates.", "ti ti-currency-dollar");
}
