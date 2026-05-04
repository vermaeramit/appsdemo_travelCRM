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

[Authorize, Route("invoices")]
[RequireFeature(Features.ModuleInvoices)]
public sealed class InvoicesController : Controller
{
    private readonly IInvoiceRepository _invoices;
    private readonly IBookingRepository _bookings;
    private readonly IBranchRepository _branches;
    private readonly INumberSequenceService _numbers;
    private readonly IAuditLogger _audit;

    public InvoicesController(IInvoiceRepository invoices, IBookingRepository bookings,
        IBranchRepository branches, INumberSequenceService numbers, IAuditLogger audit)
    {
        _invoices = invoices; _bookings = bookings; _branches = branches;
        _numbers = numbers; _audit = audit;
    }

    [HttpGet(""), HasPermission(Permissions.Invoices.View)]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 25)
    {
        var vm = new InvoiceIndexVm { Search = search, Status = status, Page = page, PageSize = pageSize };
        vm.Result = await _invoices.ListAsync(new InvoiceFilter { Search = search, Status = status, Page = page, PageSize = pageSize });
        return View("~/Views/Invoices/Index.cshtml", vm);
    }

    [HttpGet("new"), HasPermission(Permissions.Invoices.Create)]
    public async Task<IActionResult> Create(Guid? bookingId)
    {
        var vm = new InvoiceFormVm
        {
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            Status = InvoiceStatus.Draft
        };
        if (bookingId.HasValue)
        {
            var bk = await _bookings.GetByIdAsync(bookingId.Value);
            if (bk is not null)
            {
                vm.BookingId = bk.Id; vm.BookingNo = bk.BookingNo;
                vm.CustomerName = bk.CustomerName; vm.CurrencyCode = bk.CurrencyCode;
                vm.BranchId = bk.BranchId;
                vm.Items = bk.Items.Select(i => new InvoiceItemVm
                {
                    Description = i.Description ?? "",
                    Qty = i.Qty, UnitPrice = i.UnitSale, TaxRate = i.TaxRate
                }).ToList();
            }
        }
        await PopulateDropdownsAsync(vm);
        return View("~/Views/Invoices/Form.cshtml", vm);
    }

    [HttpPost("new"), ValidateAntiForgeryToken, HasPermission(Permissions.Invoices.Create)]
    public async Task<IActionResult> Create(InvoiceFormVm vm)
    {
        if (!ModelState.IsValid) { await PopulateDropdownsAsync(vm); return View("~/Views/Invoices/Form.cshtml", vm); }

        decimal subTotal = vm.Items.Sum(i => i.Qty * i.UnitPrice);
        decimal taxTotal = vm.Items.Sum(i => i.Qty * i.UnitPrice * i.TaxRate / 100m);

        var inv = new Invoice
        {
            InvoiceNo = await _numbers.NextAsync("invoice"),
            BookingId = vm.BookingId, CustomerName = vm.CustomerName,
            CustomerGstin = vm.CustomerGstin, InvoiceDate = vm.InvoiceDate,
            DueDate = vm.DueDate, SubTotal = subTotal, TaxTotal = taxTotal,
            GrandTotal = subTotal + taxTotal, CurrencyCode = vm.CurrencyCode,
            Status = vm.Status, BranchId = vm.BranchId, Notes = vm.Notes,
            CreatedBy = CurrentUserId,
            Items = vm.Items.Select((i, idx) => new InvoiceItem
            {
                Description = i.Description, HsnSac = i.HsnSac, Qty = i.Qty,
                UnitPrice = i.UnitPrice, TaxRate = i.TaxRate,
                LineTotal = i.Qty * i.UnitPrice * (1 + i.TaxRate / 100m), SortOrder = idx
            }).ToList()
        };

        var newId = await _invoices.InsertAsync(inv);
        await _audit.WriteAsync("invoice.create", "invoices", newId.ToString(), new { inv.InvoiceNo, inv.CustomerName });
        TempData["Success"] = $"Invoice {inv.InvoiceNo} created.";
        return RedirectToAction(nameof(Details), new { id = newId });
    }

    [HttpGet("{id:guid}"), HasPermission(Permissions.Invoices.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var inv = await _invoices.GetByIdAsync(id);
        if (inv is null) return NotFound();
        string? bookingNo = null;
        if (inv.BookingId.HasValue) bookingNo = (await _bookings.GetByIdAsync(inv.BookingId.Value))?.BookingNo;
        string? branchName = null;
        if (inv.BranchId.HasValue) branchName = (await _branches.GetByIdAsync(inv.BranchId.Value))?.Name;
        return View("~/Views/Invoices/Details.cshtml", new InvoiceDetailsVm
        {
            Invoice = inv, BookingNo = bookingNo, BranchName = branchName,
            CanEdit   = User.HasClaim("perm", Permissions.Invoices.Edit)   || User.IsInRole(SystemRoles.TenantAdmin),
            CanDelete = User.HasClaim("perm", Permissions.Invoices.Delete) || User.IsInRole(SystemRoles.TenantAdmin)
        });
    }

    [HttpGet("{id:guid}/edit"), HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var inv = await _invoices.GetByIdAsync(id);
        if (inv is null) return NotFound();
        var vm = new InvoiceFormVm
        {
            Id = inv.Id, InvoiceNo = inv.InvoiceNo, BookingId = inv.BookingId,
            CustomerName = inv.CustomerName, CustomerGstin = inv.CustomerGstin,
            InvoiceDate = inv.InvoiceDate, DueDate = inv.DueDate,
            Status = inv.Status, CurrencyCode = inv.CurrencyCode,
            BranchId = inv.BranchId, Notes = inv.Notes,
            Items = inv.Items.Select(i => new InvoiceItemVm
            {
                Description = i.Description, HsnSac = i.HsnSac, Qty = i.Qty,
                UnitPrice = i.UnitPrice, TaxRate = i.TaxRate, SortOrder = i.SortOrder
            }).ToList()
        };
        await PopulateDropdownsAsync(vm);
        return View("~/Views/Invoices/Form.cshtml", vm);
    }

    [HttpPost("{id:guid}/edit"), ValidateAntiForgeryToken, HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> Edit(Guid id, InvoiceFormVm vm)
    {
        if (!ModelState.IsValid) { await PopulateDropdownsAsync(vm); return View("~/Views/Invoices/Form.cshtml", vm); }
        var existing = await _invoices.GetByIdAsync(id);
        if (existing is null) return NotFound();
        decimal subTotal = vm.Items.Sum(i => i.Qty * i.UnitPrice);
        decimal taxTotal = vm.Items.Sum(i => i.Qty * i.UnitPrice * i.TaxRate / 100m);
        existing.BookingId = vm.BookingId; existing.CustomerName = vm.CustomerName;
        existing.CustomerGstin = vm.CustomerGstin; existing.InvoiceDate = vm.InvoiceDate;
        existing.DueDate = vm.DueDate; existing.SubTotal = subTotal;
        existing.TaxTotal = taxTotal; existing.GrandTotal = subTotal + taxTotal;
        existing.CurrencyCode = vm.CurrencyCode; existing.Status = vm.Status;
        existing.BranchId = vm.BranchId; existing.Notes = vm.Notes;
        existing.UpdatedBy = CurrentUserId;
        existing.Items = vm.Items.Select((i, idx) => new InvoiceItem
        {
            Description = i.Description, HsnSac = i.HsnSac, Qty = i.Qty,
            UnitPrice = i.UnitPrice, TaxRate = i.TaxRate,
            LineTotal = i.Qty * i.UnitPrice * (1 + i.TaxRate / 100m), SortOrder = idx
        }).ToList();
        await _invoices.UpdateAsync(existing);
        await _audit.WriteAsync("invoice.update", "invoices", id.ToString(), new { existing.InvoiceNo });
        TempData["Success"] = $"Invoice {existing.InvoiceNo} updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/status"), ValidateAntiForgeryToken, HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> UpdateStatus(Guid id, string status)
    {
        var inv = await _invoices.GetByIdAsync(id);
        if (inv is null) return NotFound();
        if (!InvoiceStatus.All.Contains(status)) return BadRequest();
        await _invoices.UpdateStatusAsync(id, status, CurrentUserId);
        await _audit.WriteAsync("invoice.status", "invoices", id.ToString(), new { inv.InvoiceNo, status });
        TempData["Success"] = $"Invoice marked as {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/delete"), ValidateAntiForgeryToken, HasPermission(Permissions.Invoices.Delete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var inv = await _invoices.GetByIdAsync(id);
        if (inv is null) return NotFound();
        await _invoices.SoftDeleteAsync(id, CurrentUserId);
        await _audit.WriteAsync("invoice.delete", "invoices", id.ToString(), new { inv.InvoiceNo });
        TempData["Success"] = $"Invoice {inv.InvoiceNo} deleted.";
        return RedirectToAction(nameof(Index));
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;

    private async Task PopulateDropdownsAsync(InvoiceFormVm vm)
    {
        var branches = await _branches.ListActiveAsync();
        vm.Branches = branches.Select(b => new DropdownItem { Value = b.Id.ToString(), Text = b.Name }).ToList();
        var bookings = await _bookings.ListAsync(new BookingFilter { PageSize = 200 });
        vm.Bookings = bookings.Items.Select(b => new DropdownItem
        {
            Value = b.Id.ToString(), Text = $"{b.BookingNo} · {b.CustomerName}"
        }).ToList();
    }
}
