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

[Authorize, Route("payments")]
[RequireFeature(Features.ModulePayments)]
public sealed class PaymentsController : Controller
{
    private readonly IPaymentRepository _payments;
    private readonly INumberSequenceService _numbers;
    private readonly IAuditLogger _audit;

    public PaymentsController(IPaymentRepository payments, INumberSequenceService numbers, IAuditLogger audit)
    {
        _payments = payments; _numbers = numbers; _audit = audit;
    }

    [HttpGet(""), HasPermission(Permissions.Payments.View)]
    public async Task<IActionResult> Index(string? search, string? mode, int page = 1, int pageSize = 25)
    {
        var vm = new PaymentIndexVm { Search = search, Mode = mode, Page = page, PageSize = pageSize };
        vm.Result = await _payments.ListAsync(new PaymentFilter { Search = search, Mode = mode, Page = page, PageSize = pageSize });
        return View("~/Views/Payments/Index.cshtml", vm);
    }

    [HttpGet("new"), HasPermission(Permissions.Payments.Create)]
    public IActionResult Create() =>
        View("~/Views/Payments/Form.cshtml", new PaymentFormVm
        {
            PaymentDate = DateOnly.FromDateTime(DateTime.Today),
            Mode = PaymentMode.Cash,
            CurrencyCode = "INR"
        });

    [HttpPost("new"), ValidateAntiForgeryToken, HasPermission(Permissions.Payments.Create)]
    public async Task<IActionResult> Create(PaymentFormVm vm)
    {
        if (!ModelState.IsValid) return View("~/Views/Payments/Form.cshtml", vm);

        var payment = new Payment
        {
            PaymentNo = await _numbers.NextAsync("payment"),
            PaymentDate = vm.PaymentDate, Mode = vm.Mode,
            ReferenceNo = vm.ReferenceNo, Amount = vm.Amount,
            CurrencyCode = vm.CurrencyCode, Notes = vm.Notes,
            ReceivedBy = CurrentUserId, CreatedBy = CurrentUserId
        };

        var newId = await _payments.InsertAsync(payment);
        await _audit.WriteAsync("payment.create", "payments", newId.ToString(),
            new { payment.PaymentNo, payment.Amount, payment.Mode });
        TempData["Success"] = $"Payment {payment.PaymentNo} recorded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/delete"), ValidateAntiForgeryToken, HasPermission(Permissions.Payments.Delete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var payment = await _payments.GetByIdAsync(id);
        if (payment is null) return NotFound();
        await _payments.SoftDeleteAsync(id, CurrentUserId);
        await _audit.WriteAsync("payment.delete", "payments", id.ToString(), new { payment.PaymentNo });
        TempData["Success"] = $"Payment {payment.PaymentNo} deleted.";
        return RedirectToAction(nameof(Index));
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;
}
