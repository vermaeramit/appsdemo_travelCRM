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

[Authorize, Route("branches")]
public sealed class BranchesController : Controller
{
    private readonly IBranchRepository _branches;
    private readonly IAuditLogger _audit;

    public BranchesController(IBranchRepository branches, IAuditLogger audit)
    {
        _branches = branches; _audit = audit;
    }

    [HttpGet(""), HasPermission(Permissions.Branches.View)]
    public async Task<IActionResult> Index()
    {
        var items = await _branches.ListAllAsync();
        return View("~/Views/Branches/Index.cshtml", new BranchListVm { Items = items });
    }

    [HttpGet("new"), HasPermission(Permissions.Branches.Manage)]
    public IActionResult Create() => View("~/Views/Branches/Form.cshtml", new BranchFormVm { IsActive = true, Country = "India" });

    [HttpPost("new"), ValidateAntiForgeryToken, HasPermission(Permissions.Branches.Manage)]
    public async Task<IActionResult> Create(BranchFormVm vm)
    {
        if (!ModelState.IsValid) return View("~/Views/Branches/Form.cshtml", vm);

        var branch = new Branch
        {
            Name = vm.Name, Code = vm.Code.ToUpperInvariant(),
            Address = vm.Address, City = vm.City, State = vm.State, Country = vm.Country,
            Phone = vm.Phone, Email = vm.Email, Gstin = vm.Gstin,
            IsActive = vm.IsActive, IsHeadOffice = vm.IsHeadOffice
        };

        var newId = await _branches.InsertAsync(branch, CurrentUserId);
        await _audit.WriteAsync("branch.create", "branches", newId.ToString(), new { branch.Name, branch.Code });
        TempData["Success"] = $"Branch '{branch.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/edit"), HasPermission(Permissions.Branches.Manage)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var b = await _branches.GetByIdAsync(id);
        if (b is null) return NotFound();
        return View("~/Views/Branches/Form.cshtml", new BranchFormVm
        {
            Id = b.Id, Name = b.Name, Code = b.Code,
            Address = b.Address, City = b.City, State = b.State, Country = b.Country,
            Phone = b.Phone, Email = b.Email, Gstin = b.Gstin,
            IsActive = b.IsActive, IsHeadOffice = b.IsHeadOffice
        });
    }

    [HttpPost("{id:guid}/edit"), ValidateAntiForgeryToken, HasPermission(Permissions.Branches.Manage)]
    public async Task<IActionResult> Edit(Guid id, BranchFormVm vm)
    {
        if (!ModelState.IsValid) return View("~/Views/Branches/Form.cshtml", vm);

        var branch = new Branch
        {
            Id = id, Name = vm.Name, Code = vm.Code.ToUpperInvariant(),
            Address = vm.Address, City = vm.City, State = vm.State, Country = vm.Country,
            Phone = vm.Phone, Email = vm.Email, Gstin = vm.Gstin,
            IsActive = vm.IsActive, IsHeadOffice = vm.IsHeadOffice
        };

        await _branches.UpdateAsync(branch, CurrentUserId);
        await _audit.WriteAsync("branch.update", "branches", id.ToString(), new { branch.Name });
        TempData["Success"] = $"Branch '{branch.Name}' updated.";
        return RedirectToAction(nameof(Index));
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;
}
