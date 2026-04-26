using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers.Admin;

public abstract class AdminPlaceholderController : Controller
{
    protected IActionResult ComingSoon(string title, string description, string icon = "ti ti-rocket")
    {
        ViewData["Title"] = title;
        ViewData["Description"] = description;
        ViewData["Icon"] = icon;
        return View("~/Views/Shared/ComingSoon.cshtml");
    }
}

[Authorize(Policy = "SuperAdmin"), Route("admin/plans")]
public sealed class PlansController : AdminPlaceholderController
{
    [HttpGet("")]
    public IActionResult Index() => ComingSoon("Subscription Plans",
        "Define and edit Starter / Pro / Enterprise plans.", "ti ti-package");
}

[Authorize(Policy = "SuperAdmin"), Route("admin/features")]
public sealed class FeaturesController : AdminPlaceholderController
{
    [HttpGet("")]
    public IActionResult Index() => ComingSoon("Features",
        "Catalog of all features the app exposes (used by plans).", "ti ti-checkbox");
}

[Authorize(Policy = "SuperAdmin"), Route("admin/billing")]
public sealed class BillingController : AdminPlaceholderController
{
    [HttpGet("")]
    public IActionResult Index() => ComingSoon("Tenant Invoices",
        "Subscription invoices issued to tenants.", "ti ti-receipt-tax");
}

[Authorize(Policy = "SuperAdmin"), Route("admin/audit-global")]
public sealed class GlobalAuditController : AdminPlaceholderController
{
    [HttpGet("")]
    public IActionResult Index() => ComingSoon("Global Audit Log",
        "Every super-admin action across the platform.", "ti ti-history");
}
