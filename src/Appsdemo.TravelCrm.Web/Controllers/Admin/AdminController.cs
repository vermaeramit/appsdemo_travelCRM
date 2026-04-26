using Appsdemo.TravelCrm.Data.Repositories.Master;
using Appsdemo.TravelCrm.Web.Models.ViewModels;
using Appsdemo.TravelCrm.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers.Admin;

[Route("admin"), Authorize(Policy = "SuperAdmin")]
public sealed class AdminController : Controller
{
    private readonly ITenantRepository _tenants;
    private readonly ISubscriptionPlanRepository _plans;
    private readonly ITenantProvisioner _provisioner;
    private readonly IAuditLogger _audit;

    public AdminController(
        ITenantRepository tenants,
        ISubscriptionPlanRepository plans,
        ITenantProvisioner provisioner,
        IAuditLogger audit)
    {
        _tenants = tenants;
        _plans = plans;
        _provisioner = provisioner;
        _audit = audit;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 25)
    {
        var items = await _tenants.ListAsync(search, page, pageSize);
        var total = await _tenants.CountAsync(search);
        return View("~/Views/Admin/Index.cshtml", new TenantListVm
        {
            Items = items, Search = search, Page = page, PageSize = pageSize, TotalCount = total
        });
    }

    [HttpGet("tenants/new")]
    public async Task<IActionResult> NewTenant()
    {
        var plans = await _plans.ListActiveAsync();
        return View("~/Views/Admin/NewTenant.cshtml", new CreateTenantVm
        {
            AvailablePlans = plans,
            PlanId = plans.FirstOrDefault()?.Id ?? Guid.Empty
        });
    }

    [HttpPost("tenants/new"), ValidateAntiForgeryToken]
    public async Task<IActionResult> NewTenant(CreateTenantVm vm)
    {
        vm.AvailablePlans = await _plans.ListActiveAsync();
        if (!ModelState.IsValid)
            return View("~/Views/Admin/NewTenant.cshtml", vm);

        try
        {
            var actorId = Guid.TryParse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var g)
                ? g : Guid.Empty;
            var tenant = await _provisioner.ProvisionAsync(new NewTenantRequest(
                vm.Code, vm.CompanyName, vm.ContactPerson, vm.Email, vm.Phone,
                vm.Country, vm.Timezone, vm.CurrencyCode,
                vm.PlanId, vm.AdminUserFullName, vm.AdminUserEmail, vm.AdminUserPassword), actorId);

            await _audit.WriteGlobalAsync("tenant.create", "tenants", tenant.Id.ToString(),
                new { tenant.Code, tenant.CompanyName });
            TempData["Success"] = $"Tenant '{tenant.Code}' provisioned.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("~/Views/Admin/NewTenant.cshtml", vm);
        }
    }
}
