using System.Security.Claims;
using System.Text.Json;
using Appsdemo.TravelCrm.Core.Models.Tenant;
using Appsdemo.TravelCrm.Core.Security;
using Appsdemo.TravelCrm.Data.Repositories.Tenant;
using Appsdemo.TravelCrm.Data.Services;
using Appsdemo.TravelCrm.Web.Services;
using Appsdemo.TravelCrm.Web.Authorization;
using Appsdemo.TravelCrm.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appsdemo.TravelCrm.Web.Controllers;

[Authorize, Route("quotes")]
[RequireFeature(Features.ModuleQuotes)]
public sealed class QuotesController : Controller
{
    private readonly IQuoteRepository _quotes;
    private readonly ILeadRepository _leads;
    private readonly IBranchRepository _branches;
    private readonly INumberSequenceService _numbers;
    private readonly IAuditLogger _audit;

    public QuotesController(
        IQuoteRepository quotes,
        ILeadRepository leads,
        IBranchRepository branches,
        INumberSequenceService numbers,
        IAuditLogger audit)
    {
        _quotes = quotes;
        _leads = leads;
        _branches = branches;
        _numbers = numbers;
        _audit = audit;
    }

    // ---------------- LIST ----------------
    [HttpGet("")]
    [HasPermission(Permissions.Quotes.View)]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 25)
    {
        var vm = new QuoteIndexVm { Search = search, Status = status, Page = page, PageSize = pageSize };
        vm.Result = await _quotes.ListAsync(new QuoteFilter
        {
            Search = search, Status = status, Page = page, PageSize = pageSize
        });
        return View("~/Views/Quotes/Index.cshtml", vm);
    }

    // ---------------- CREATE ----------------
    [HttpGet("new")]
    [HasPermission(Permissions.Quotes.Create)]
    public async Task<IActionResult> Create(Guid? leadId)
    {
        var vm = new QuoteFormVm { Status = QuoteStatus.Draft };
        if (leadId.HasValue)
        {
            var lead = await _leads.GetByIdAsync(leadId.Value);
            if (lead is not null)
            {
                vm.LeadId = lead.Id;
                vm.CustomerNameDisplay = lead.CustomerName;
                vm.CurrencyCode = lead.CurrencyCode;
            }
        }
        await PopulateDropdownsAsync(vm);
        return View("~/Views/Quotes/Form.cshtml", vm);
    }

    [HttpPost("new"), ValidateAntiForgeryToken]
    [HasPermission(Permissions.Quotes.Create)]
    public async Task<IActionResult> Create(QuoteFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(vm);
            return View("~/Views/Quotes/Form.cshtml", vm);
        }

        var (totalCost, totalSale, totalTax, grandTotal) = CalcTotals(vm.Items);

        string? customerName = null;
        if (!vm.LeadId.HasValue && !string.IsNullOrWhiteSpace(vm.CustomerNameDisplay))
            customerName = vm.CustomerNameDisplay;

        var quote = new Quote
        {
            QuoteNo = await _numbers.NextAsync("quote"),
            LeadId = vm.LeadId,
            CustomerName = customerName,
            Version = 1,
            ValidTill = vm.ValidTill,
            Status = vm.Status,
            CurrencyCode = vm.CurrencyCode,
            BranchId = vm.BranchId,
            Notes = vm.Notes,
            TotalCost = totalCost,
            TotalSale = totalSale,
            TotalTax = totalTax,
            GrandTotal = grandTotal,
            CreatedBy = CurrentUserId,
            Items = vm.Items.Select(ToQuoteItem).ToList(),
            Terms = vm.Terms
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => new QuoteTerm { TermsText = t })
                .ToList()
        };

        var newId = await _quotes.InsertAsync(quote);
        await _audit.WriteAsync("quote.create", "quotes", newId.ToString(),
            new { quote.QuoteNo, quote.Status });

        TempData["Success"] = $"Quote {quote.QuoteNo} created.";
        return RedirectToAction(nameof(Details), new { id = newId });
    }

    // ---------------- EDIT ----------------
    [HttpGet("{id:guid}/edit")]
    [HasPermission(Permissions.Quotes.Edit)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var quote = await _quotes.GetByIdAsync(id);
        if (quote is null) return NotFound();

        var vm = ToFormVm(quote);
        await PopulateDropdownsAsync(vm);
        return View("~/Views/Quotes/Form.cshtml", vm);
    }

    [HttpPost("{id:guid}/edit"), ValidateAntiForgeryToken]
    [HasPermission(Permissions.Quotes.Edit)]
    public async Task<IActionResult> Edit(Guid id, QuoteFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(vm);
            return View("~/Views/Quotes/Form.cshtml", vm);
        }

        var existing = await _quotes.GetByIdAsync(id);
        if (existing is null) return NotFound();

        var (totalCost, totalSale, totalTax, grandTotal) = CalcTotals(vm.Items);

        string? customerName = null;
        if (!vm.LeadId.HasValue && !string.IsNullOrWhiteSpace(vm.CustomerNameDisplay))
            customerName = vm.CustomerNameDisplay;

        existing.LeadId = vm.LeadId;
        existing.CustomerName = customerName;
        existing.ValidTill = vm.ValidTill;
        existing.Status = vm.Status;
        existing.CurrencyCode = vm.CurrencyCode;
        existing.BranchId = vm.BranchId;
        existing.Notes = vm.Notes;
        existing.TotalCost = totalCost;
        existing.TotalSale = totalSale;
        existing.TotalTax = totalTax;
        existing.GrandTotal = grandTotal;
        existing.UpdatedBy = CurrentUserId;
        existing.Items = vm.Items.Select(ToQuoteItem).ToList();
        existing.Terms = vm.Terms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => new QuoteTerm { TermsText = t })
            .ToList();

        await _quotes.UpdateAsync(existing);
        await _audit.WriteAsync("quote.update", "quotes", id.ToString(),
            new { existing.QuoteNo, existing.Status });

        TempData["Success"] = $"Quote {existing.QuoteNo} updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------------- DETAILS ----------------
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Quotes.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var quote = await _quotes.GetByIdAsync(id);
        if (quote is null) return NotFound();

        string? leadNo = null;
        if (quote.LeadId.HasValue)
        {
            var lead = await _leads.GetByIdAsync(quote.LeadId.Value);
            leadNo = lead?.LeadNo;
        }

        string? branchName = null;
        if (quote.BranchId.HasValue)
            branchName = (await _branches.GetByIdAsync(quote.BranchId.Value))?.Name;

        var vm = new QuoteDetailsVm
        {
            Quote = quote,
            LeadNo = leadNo,
            BranchName = branchName,
            CanEdit    = User.HasClaim("perm", Permissions.Quotes.Edit)    || User.IsInRole(SystemRoles.TenantAdmin),
            CanApprove = User.HasClaim("perm", Permissions.Quotes.Approve) || User.IsInRole(SystemRoles.TenantAdmin),
            CanSend    = User.HasClaim("perm", Permissions.Quotes.Send)    || User.IsInRole(SystemRoles.TenantAdmin),
            CanDelete  = User.HasClaim("perm", Permissions.Quotes.Delete)  || User.IsInRole(SystemRoles.TenantAdmin),
        };
        return View("~/Views/Quotes/Details.cshtml", vm);
    }

    // ---------------- STATUS CHANGE ----------------
    [HttpPost("{id:guid}/status"), ValidateAntiForgeryToken]
    [HasPermission(Permissions.Quotes.Edit)]
    public async Task<IActionResult> UpdateStatus(Guid id, string status)
    {
        var quote = await _quotes.GetByIdAsync(id);
        if (quote is null) return NotFound();

        if (!QuoteStatus.All.Contains(status))
            return BadRequest("Invalid status.");

        await _quotes.UpdateStatusAsync(id, status, CurrentUserId);
        await _audit.WriteAsync("quote.status", "quotes", id.ToString(),
            new { quote.QuoteNo, OldStatus = quote.Status, NewStatus = status });

        TempData["Success"] = $"Quote {quote.QuoteNo} marked as {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------------- DELETE ----------------
    [HttpPost("{id:guid}/delete"), ValidateAntiForgeryToken]
    [HasPermission(Permissions.Quotes.Delete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var quote = await _quotes.GetByIdAsync(id);
        if (quote is null) return NotFound();

        await _quotes.SoftDeleteAsync(id, CurrentUserId);
        await _audit.WriteAsync("quote.delete", "quotes", id.ToString(),
            new { quote.QuoteNo });

        TempData["Success"] = $"Quote {quote.QuoteNo} deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- helpers ----------------
    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;

    private async Task PopulateDropdownsAsync(QuoteFormVm vm)
    {
        var branches = await _branches.ListActiveAsync();
        vm.Branches = branches.Select(b => new DropdownItem { Value = b.Id.ToString(), Text = b.Name }).ToList();

        var leads = await _leads.ListAsync(new LeadFilter { PageSize = 500 });
        vm.Leads = leads.Items.Select(l => new DropdownItem
        {
            Value = l.Id.ToString(),
            Text = $"{l.LeadNo} · {l.CustomerName}"
        }).ToList();
    }

    private static (decimal totalCost, decimal totalSale, decimal totalTax, decimal grandTotal)
        CalcTotals(IEnumerable<QuoteItemVm> items)
    {
        decimal cost = 0, sale = 0, tax = 0;
        foreach (var i in items)
        {
            cost += i.Qty * i.UnitCost;
            sale += i.Qty * i.UnitSale;
            tax  += i.Qty * i.UnitSale * i.TaxRate / 100m;
        }
        return (cost, sale, tax, sale + tax);
    }

    private static QuoteItem ToQuoteItem(QuoteItemVm vm) => new()
    {
        ItemType    = vm.ItemType,
        DayNo       = vm.DayNo,
        ItemDate    = vm.ItemDate,
        Description = vm.Description,
        Qty         = vm.Qty,
        UnitCost    = vm.UnitCost,
        UnitSale    = vm.UnitSale,
        TaxRate     = vm.TaxRate,
        LineTotal   = vm.Qty * vm.UnitSale * (1 + vm.TaxRate / 100m),
        SortOrder   = vm.SortOrder
    };

    private static QuoteFormVm ToFormVm(Quote q) => new()
    {
        Id     = q.Id,
        QuoteNo = q.QuoteNo,
        LeadId = q.LeadId,
        CustomerNameDisplay = q.CustomerName,
        ValidTill    = q.ValidTill,
        Status       = q.Status,
        CurrencyCode = q.CurrencyCode,
        BranchId     = q.BranchId,
        Notes        = q.Notes,
        Items = q.Items.Select(i => new QuoteItemVm
        {
            DayNo       = i.DayNo,
            ItemDate    = i.ItemDate,
            ItemType    = i.ItemType ?? QuoteItemType.Misc,
            Description = i.Description,
            Qty         = i.Qty,
            UnitCost    = i.UnitCost,
            UnitSale    = i.UnitSale,
            TaxRate     = i.TaxRate,
            SortOrder   = i.SortOrder
        }).ToList(),
        Terms = q.Terms.Select(t => t.TermsText).ToList()
    };
}
