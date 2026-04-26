using System.Security.Claims;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Core.Security;
using Appsdemo.TravelCrm.Data.Repositories.Tenant;
using Appsdemo.TravelCrm.Data.Services;
using Appsdemo.TravelCrm.Web.Authorization;
using Appsdemo.TravelCrm.Web.Models.ViewModels;
using Appsdemo.TravelCrm.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers;

[Authorize, Route("leads")]
[RequireFeature(Features.ModuleCrm)]
public sealed class LeadsController : Controller
{
    private readonly ILeadRepository _leads;
    private readonly IUserRepository _users;
    private readonly IBranchRepository _branches;
    private readonly INumberSequenceService _numbers;
    private readonly IAuditLogger _audit;

    public LeadsController(
        ILeadRepository leads,
        IUserRepository users,
        IBranchRepository branches,
        INumberSequenceService numbers,
        IAuditLogger audit)
    {
        _leads = leads;
        _users = users;
        _branches = branches;
        _numbers = numbers;
        _audit = audit;
    }

    // ---------------- LIST ----------------
    [HttpGet("")]
    [HasPermission(Permissions.Leads.View)]
    public async Task<IActionResult> Index(
        string? search, string? status, Guid? assignedTo, Guid? branchId,
        int page = 1, int pageSize = 25)
    {
        var vm = new LeadIndexVm
        {
            Search = search, Status = status,
            AssignedTo = assignedTo, BranchId = branchId,
            Page = page, PageSize = pageSize
        };

        vm.Result = await _leads.ListAsync(new LeadFilter
        {
            Search = search, Status = status,
            AssignedTo = assignedTo, BranchId = branchId,
            Page = page, PageSize = pageSize
        });
        await PopulateDropdownsAsync(vm);
        return View("~/Views/Leads/Index.cshtml", vm);
    }

    private async Task PopulateDropdownsAsync(LeadIndexVm vm)
    {
        var users = await _users.ListAsync(null, 1, 1000);
        vm.Users = users.Where(u => u.IsActive).Select(u => new DropdownItem
        { Value = u.Id.ToString(), Text = u.FullName }).ToList();

        var branches = await _branches.ListActiveAsync();
        vm.Branches = branches.Select(b => new DropdownItem
        { Value = b.Id.ToString(), Text = b.Name }).ToList();
    }

    // ---------------- CREATE ----------------
    [HttpGet("new")]
    [HasPermission(Permissions.Leads.Create)]
    public async Task<IActionResult> Create()
    {
        var vm = new LeadFormVm { AssignedTo = CurrentUserId };
        await PopulateFormDropdownsAsync(vm);
        return View("~/Views/Leads/Form.cshtml", vm);
    }

    [HttpPost("new"), ValidateAntiForgeryToken]
    [HasPermission(Permissions.Leads.Create)]
    public async Task<IActionResult> Create(LeadFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormDropdownsAsync(vm);
            return View("~/Views/Leads/Form.cshtml", vm);
        }

        var lead = new Lead
        {
            LeadNo = await _numbers.NextAsync("lead"),
            Source = vm.Source, CustomerName = vm.CustomerName,
            Email = vm.Email, Phone = vm.Phone,
            TravelDate = vm.TravelDate, TravelNights = vm.TravelNights,
            PaxAdults = vm.PaxAdults, PaxChildren = vm.PaxChildren,
            Budget = vm.Budget, CurrencyCode = vm.CurrencyCode,
            Status = vm.Status, LostReason = vm.LostReason,
            AssignedTo = vm.AssignedTo, BranchId = vm.BranchId,
            Notes = vm.Notes, CreatedBy = CurrentUserId
        };

        var newId = await _leads.InsertAsync(lead);
        await _audit.WriteAsync("lead.create", "leads", newId.ToString(),
            new { lead.LeadNo, lead.CustomerName });

        TempData["Success"] = $"Lead {lead.LeadNo} created.";
        return RedirectToAction(nameof(Details), new { id = newId });
    }

    // ---------------- EDIT ----------------
    [HttpGet("{id:guid}/edit")]
    [HasPermission(Permissions.Leads.Edit)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var lead = await _leads.GetByIdAsync(id);
        if (lead is null) return NotFound();

        var vm = ToFormVm(lead);
        await PopulateFormDropdownsAsync(vm);
        return View("~/Views/Leads/Form.cshtml", vm);
    }

    [HttpPost("{id:guid}/edit"), ValidateAntiForgeryToken]
    [HasPermission(Permissions.Leads.Edit)]
    public async Task<IActionResult> Edit(Guid id, LeadFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormDropdownsAsync(vm);
            return View("~/Views/Leads/Form.cshtml", vm);
        }

        var existing = await _leads.GetByIdAsync(id);
        if (existing is null) return NotFound();

        existing.Source = vm.Source;
        existing.CustomerName = vm.CustomerName;
        existing.Email = vm.Email;
        existing.Phone = vm.Phone;
        existing.TravelDate = vm.TravelDate;
        existing.TravelNights = vm.TravelNights;
        existing.PaxAdults = vm.PaxAdults;
        existing.PaxChildren = vm.PaxChildren;
        existing.Budget = vm.Budget;
        existing.CurrencyCode = vm.CurrencyCode;
        existing.Status = vm.Status;
        existing.LostReason = vm.LostReason;
        existing.AssignedTo = vm.AssignedTo;
        existing.BranchId = vm.BranchId;
        existing.Notes = vm.Notes;
        existing.UpdatedBy = CurrentUserId;

        await _leads.UpdateAsync(existing);
        await _audit.WriteAsync("lead.update", "leads", id.ToString(),
            new { existing.LeadNo, existing.Status });

        TempData["Success"] = $"Lead {existing.LeadNo} updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------------- DETAILS ----------------
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Leads.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var lead = await _leads.GetByIdAsync(id);
        if (lead is null) return NotFound();

        var followups = await _leads.ListFollowupsAsync(id);

        string? assignedToName = null;
        if (lead.AssignedTo.HasValue)
            assignedToName = (await _users.GetByIdAsync(lead.AssignedTo.Value))?.FullName;

        string? branchName = null;
        if (lead.BranchId.HasValue)
            branchName = (await _branches.GetByIdAsync(lead.BranchId.Value))?.Name;

        return View("~/Views/Leads/Details.cshtml", new LeadDetailsVm
        {
            Lead = lead,
            Followups = followups,
            AssignedToName = assignedToName,
            BranchName = branchName,
            NewFollowup = new NewFollowupVm { LeadId = id }
        });
    }

    // ---------------- ADD FOLLOW-UP ----------------
    [HttpPost("{id:guid}/followups"), ValidateAntiForgeryToken]
    [HasPermission(Permissions.Leads.Edit)]
    public async Task<IActionResult> AddFollowup(Guid id, NewFollowupVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Success"] = null;
            return RedirectToAction(nameof(Details), new { id });
        }

        var lead = await _leads.GetByIdAsync(id);
        if (lead is null) return NotFound();

        await _leads.InsertFollowupAsync(new LeadFollowup
        {
            LeadId = id,
            FollowupDate = ToUtcOffset(vm.FollowupDate),
            Mode = vm.Mode,
            Notes = vm.Notes,
            NextFollowupDate = vm.NextFollowupDate.HasValue
                ? ToUtcOffset(vm.NextFollowupDate.Value)
                : null,
            DoneBy = CurrentUserId
        });
        await _audit.WriteAsync("lead.followup.add", "leads", id.ToString(),
            new { vm.Mode, vm.NextFollowupDate });

        TempData["Success"] = "Follow-up added.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------------- DELETE ----------------
    [HttpPost("{id:guid}/delete"), ValidateAntiForgeryToken]
    [HasPermission(Permissions.Leads.Delete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var lead = await _leads.GetByIdAsync(id);
        if (lead is null) return NotFound();

        await _leads.SoftDeleteAsync(id, CurrentUserId);
        await _audit.WriteAsync("lead.delete", "leads", id.ToString(),
            new { lead.LeadNo, lead.CustomerName });

        TempData["Success"] = $"Lead {lead.LeadNo} deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- helpers ----------------
    // Form datetime-local fields arrive as Kind=Unspecified; treat as the
    // server's local time and convert to UTC for Npgsql 9 / timestamptz.
    private static DateTimeOffset ToUtcOffset(DateTime dt)
    {
        var local = dt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dt, DateTimeKind.Local)
            : dt;
        return new DateTimeOffset(local).ToUniversalTime();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;

    private async Task PopulateFormDropdownsAsync(LeadFormVm vm)
    {
        var users = await _users.ListAsync(null, 1, 1000);
        vm.Users = users.Where(u => u.IsActive).Select(u => new DropdownItem
        { Value = u.Id.ToString(), Text = u.FullName }).ToList();

        var branches = await _branches.ListActiveAsync();
        vm.Branches = branches.Select(b => new DropdownItem
        { Value = b.Id.ToString(), Text = b.Name }).ToList();
    }

    private static LeadFormVm ToFormVm(Lead l) => new()
    {
        Id = l.Id, LeadNo = l.LeadNo,
        Source = l.Source, CustomerName = l.CustomerName,
        Email = l.Email, Phone = l.Phone,
        TravelDate = l.TravelDate, TravelNights = l.TravelNights,
        PaxAdults = l.PaxAdults, PaxChildren = l.PaxChildren,
        Budget = l.Budget, CurrencyCode = l.CurrencyCode,
        Status = l.Status, LostReason = l.LostReason,
        AssignedTo = l.AssignedTo, BranchId = l.BranchId,
        Notes = l.Notes
    };
}
