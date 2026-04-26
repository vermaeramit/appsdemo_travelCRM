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

[Authorize, Route("leads")]
public sealed class LeadsController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Leads.View), RequireFeature(Features.ModuleCrm)]
    public IActionResult Index() => ComingSoon("Leads",
        "Capture, qualify and follow up on inbound enquiries.", "ti ti-target-arrow");
}

[Authorize, Route("quotes")]
public sealed class QuotesController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Quotes.View), RequireFeature(Features.ModuleQuotes)]
    public IActionResult Index() => ComingSoon("Quotes",
        "Build itinerary cost sheets, version them, and send PDFs.", "ti ti-file-invoice");
}

[Authorize, Route("bookings")]
public sealed class BookingsController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Bookings.View), RequireFeature(Features.ModuleBookings)]
    public IActionResult Index() => ComingSoon("Bookings",
        "Confirm itineraries and track operations.", "ti ti-calendar-event");
}

[Authorize, Route("vouchers")]
public sealed class VouchersController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Vouchers.View), RequireFeature(Features.ModuleVouchers)]
    public IActionResult Index() => ComingSoon("Vouchers",
        "Generate supplier vouchers for hotels, transport, sightseeing.", "ti ti-ticket");
}

[Authorize, Route("invoices")]
public sealed class InvoicesController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Invoices.View), RequireFeature(Features.ModuleInvoices)]
    public IActionResult Index() => ComingSoon("Invoices",
        "Customer invoices with GST, ageing, and email.", "ti ti-receipt");
}

[Authorize, Route("payments")]
public sealed class PaymentsController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Payments.View), RequireFeature(Features.ModulePayments)]
    public IActionResult Index() => ComingSoon("Payments",
        "Record receipts, allocate to invoices, view ageing.", "ti ti-cash");
}

[Authorize, Route("reports")]
public sealed class ReportsController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Reports.View), RequireFeature(Features.ModuleReports)]
    public IActionResult Index() => ComingSoon("Reports",
        "Sales, operations and financial reports.", "ti ti-chart-bar");
}

[Authorize, Route("branches")]
public sealed class BranchesController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Branches.View)]
    public IActionResult Index() => ComingSoon("Branches",
        "Manage branches, GSTINs and head-office settings.", "ti ti-building-bank");
}

[Authorize, Route("settings")]
public sealed class SettingsController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Settings.View)]
    public IActionResult Index() => ComingSoon("Settings",
        "Tenant preferences, number sequences, email templates.", "ti ti-adjustments");
}

[Authorize, Route("audit")]
public sealed class AuditController : PlaceholderController
{
    [HttpGet(""), HasPermission(Permissions.Audit.View)]
    public IActionResult Index() => ComingSoon("Audit Log",
        "Every sensitive action, who did it, when, and from where.", "ti ti-history");
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
