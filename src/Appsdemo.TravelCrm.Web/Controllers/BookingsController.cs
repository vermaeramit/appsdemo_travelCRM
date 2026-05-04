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

[Authorize, Route("bookings")]
[RequireFeature(Features.ModuleBookings)]
public sealed class BookingsController : Controller
{
    private readonly IBookingRepository _bookings;
    private readonly IQuoteRepository _quotes;
    private readonly IBranchRepository _branches;
    private readonly INumberSequenceService _numbers;
    private readonly IAuditLogger _audit;

    public BookingsController(IBookingRepository bookings, IQuoteRepository quotes,
        IBranchRepository branches, INumberSequenceService numbers, IAuditLogger audit)
    {
        _bookings = bookings; _quotes = quotes; _branches = branches;
        _numbers = numbers; _audit = audit;
    }

    [HttpGet(""), HasPermission(Permissions.Bookings.View)]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 25)
    {
        var vm = new BookingIndexVm { Search = search, Status = status, Page = page, PageSize = pageSize };
        vm.Result = await _bookings.ListAsync(new BookingFilter { Search = search, Status = status, Page = page, PageSize = pageSize });
        return View("~/Views/Bookings/Index.cshtml", vm);
    }

    [HttpGet("new"), HasPermission(Permissions.Bookings.Create)]
    public async Task<IActionResult> Create(Guid? quoteId)
    {
        var vm = new BookingFormVm { Status = BookingStatus.Confirmed };
        if (quoteId.HasValue)
        {
            var q = await _quotes.GetByIdAsync(quoteId.Value);
            if (q is not null)
            {
                vm.QuoteId = q.Id; vm.QuoteNo = q.QuoteNo;
                vm.CustomerName = q.CustomerName ?? "";
                vm.CurrencyCode = q.CurrencyCode;
                vm.BranchId = q.BranchId;
                vm.Items = q.Items.Select(i => new BookingItemVm
                {
                    DayNo = i.DayNo, ItemDate = i.ItemDate, ItemType = i.ItemType ?? "Misc",
                    Description = i.Description, Qty = i.Qty,
                    UnitCost = i.UnitCost, UnitSale = i.UnitSale, TaxRate = i.TaxRate
                }).ToList();
            }
        }
        await PopulateDropdownsAsync(vm);
        return View("~/Views/Bookings/Form.cshtml", vm);
    }

    [HttpPost("new"), ValidateAntiForgeryToken, HasPermission(Permissions.Bookings.Create)]
    public async Task<IActionResult> Create(BookingFormVm vm)
    {
        if (!ModelState.IsValid) { await PopulateDropdownsAsync(vm); return View("~/Views/Bookings/Form.cshtml", vm); }

        decimal total = vm.Items.Sum(i => i.Qty * i.UnitSale * (1 + i.TaxRate / 100m));

        var bk = new Booking
        {
            BookingNo = await _numbers.NextAsync("booking"),
            QuoteId = vm.QuoteId, CustomerName = vm.CustomerName,
            TravelStart = vm.TravelStart, TravelEnd = vm.TravelEnd,
            Status = vm.Status, TotalAmount = total,
            PaidAmount = 0, BalanceAmount = total,
            CurrencyCode = vm.CurrencyCode, BranchId = vm.BranchId,
            Notes = vm.Notes, CreatedBy = CurrentUserId,
            Items = vm.Items.Select((i, idx) => new BookingItem
            {
                ItemType = i.ItemType, DayNo = i.DayNo, ItemDate = i.ItemDate,
                Description = i.Description, Qty = i.Qty,
                UnitCost = i.UnitCost, UnitSale = i.UnitSale, TaxRate = i.TaxRate,
                LineTotal = i.Qty * i.UnitSale * (1 + i.TaxRate / 100m), SortOrder = idx
            }).ToList()
        };

        var newId = await _bookings.InsertAsync(bk);
        if (vm.QuoteId.HasValue)
            await _quotes.UpdateStatusAsync(vm.QuoteId.Value, QuoteStatus.Won, CurrentUserId);

        await _audit.WriteAsync("booking.create", "bookings", newId.ToString(), new { bk.BookingNo, bk.CustomerName });
        TempData["Success"] = $"Booking {bk.BookingNo} created.";
        return RedirectToAction(nameof(Details), new { id = newId });
    }

    [HttpGet("{id:guid}"), HasPermission(Permissions.Bookings.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var bk = await _bookings.GetByIdAsync(id);
        if (bk is null) return NotFound();
        string? quoteNo = null;
        if (bk.QuoteId.HasValue) quoteNo = (await _quotes.GetByIdAsync(bk.QuoteId.Value))?.QuoteNo;
        string? branchName = null;
        if (bk.BranchId.HasValue) branchName = (await _branches.GetByIdAsync(bk.BranchId.Value))?.Name;
        return View("~/Views/Bookings/Details.cshtml", new BookingDetailsVm
        {
            Booking = bk, QuoteNo = quoteNo, BranchName = branchName,
            CanEdit   = User.HasClaim("perm", Permissions.Bookings.Edit)   || User.IsInRole(SystemRoles.TenantAdmin),
            CanDelete = User.HasClaim("perm", Permissions.Bookings.Cancel) || User.IsInRole(SystemRoles.TenantAdmin)
        });
    }

    [HttpGet("{id:guid}/edit"), HasPermission(Permissions.Bookings.Edit)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var bk = await _bookings.GetByIdAsync(id);
        if (bk is null) return NotFound();
        var vm = new BookingFormVm
        {
            Id = bk.Id, BookingNo = bk.BookingNo, QuoteId = bk.QuoteId,
            CustomerName = bk.CustomerName, TravelStart = bk.TravelStart, TravelEnd = bk.TravelEnd,
            Status = bk.Status, CurrencyCode = bk.CurrencyCode, BranchId = bk.BranchId, Notes = bk.Notes,
            Items = bk.Items.Select(i => new BookingItemVm
            {
                DayNo = i.DayNo, ItemDate = i.ItemDate, ItemType = i.ItemType ?? "Misc",
                Description = i.Description, Qty = i.Qty,
                UnitCost = i.UnitCost, UnitSale = i.UnitSale, TaxRate = i.TaxRate, SortOrder = i.SortOrder
            }).ToList()
        };
        await PopulateDropdownsAsync(vm);
        return View("~/Views/Bookings/Form.cshtml", vm);
    }

    [HttpPost("{id:guid}/edit"), ValidateAntiForgeryToken, HasPermission(Permissions.Bookings.Edit)]
    public async Task<IActionResult> Edit(Guid id, BookingFormVm vm)
    {
        if (!ModelState.IsValid) { await PopulateDropdownsAsync(vm); return View("~/Views/Bookings/Form.cshtml", vm); }
        var existing = await _bookings.GetByIdAsync(id);
        if (existing is null) return NotFound();
        decimal total = vm.Items.Sum(i => i.Qty * i.UnitSale * (1 + i.TaxRate / 100m));
        existing.CustomerName = vm.CustomerName; existing.TravelStart = vm.TravelStart;
        existing.TravelEnd = vm.TravelEnd; existing.Status = vm.Status;
        existing.TotalAmount = total; existing.BalanceAmount = total - existing.PaidAmount;
        existing.CurrencyCode = vm.CurrencyCode; existing.BranchId = vm.BranchId;
        existing.Notes = vm.Notes; existing.UpdatedBy = CurrentUserId;
        existing.Items = vm.Items.Select((i, idx) => new BookingItem
        {
            ItemType = i.ItemType, DayNo = i.DayNo, ItemDate = i.ItemDate,
            Description = i.Description, Qty = i.Qty,
            UnitCost = i.UnitCost, UnitSale = i.UnitSale, TaxRate = i.TaxRate,
            LineTotal = i.Qty * i.UnitSale * (1 + i.TaxRate / 100m), SortOrder = idx
        }).ToList();
        await _bookings.UpdateAsync(existing);
        await _audit.WriteAsync("booking.update", "bookings", id.ToString(), new { existing.BookingNo });
        TempData["Success"] = $"Booking {existing.BookingNo} updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/status"), ValidateAntiForgeryToken, HasPermission(Permissions.Bookings.Edit)]
    public async Task<IActionResult> UpdateStatus(Guid id, string status)
    {
        var bk = await _bookings.GetByIdAsync(id);
        if (bk is null) return NotFound();
        if (!BookingStatus.All.Contains(status)) return BadRequest();
        await _bookings.UpdateStatusAsync(id, status, CurrentUserId);
        await _audit.WriteAsync("booking.status", "bookings", id.ToString(), new { bk.BookingNo, status });
        TempData["Success"] = $"Booking marked as {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/cancel"), ValidateAntiForgeryToken, HasPermission(Permissions.Bookings.Cancel)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var bk = await _bookings.GetByIdAsync(id);
        if (bk is null) return NotFound();
        await _bookings.UpdateStatusAsync(id, BookingStatus.Cancelled, CurrentUserId);
        await _audit.WriteAsync("booking.cancel", "bookings", id.ToString(), new { bk.BookingNo });
        TempData["Success"] = $"Booking {bk.BookingNo} cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;

    private async Task PopulateDropdownsAsync(BookingFormVm vm)
    {
        var branches = await _branches.ListActiveAsync();
        vm.Branches = branches.Select(b => new DropdownItem { Value = b.Id.ToString(), Text = b.Name }).ToList();
    }
}
