using System.Security.Claims;
using Appsdemo.TravelCrm.Data.Repositories.Master;
using Appsdemo.TravelCrm.Web.Configuration;
using Appsdemo.TravelCrm.Web.Models.ViewModels;
using Appsdemo.TravelCrm.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Appsdemo.TravelCrm.Web.Controllers.Admin;

[Route("admin")]
public sealed class AdminAuthController : Controller
{
    private readonly IGlobalUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly AuthOptions _opt;
    private readonly IAuditLogger _audit;

    public AdminAuthController(
        IGlobalUserRepository users,
        IPasswordHasher hasher,
        IOptions<AuthOptions> opt,
        IAuditLogger audit)
    {
        _users = users;
        _hasher = hasher;
        _opt = opt.Value;
        _audit = audit;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
        => View("~/Views/AdminAuth/Login.cshtml", new AdminLoginVm { ReturnUrl = returnUrl });

    [HttpPost("login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginVm vm)
    {
        if (!ModelState.IsValid) return View("~/Views/AdminAuth/Login.cshtml", vm);

        var user = await _users.GetByEmailAsync(vm.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View("~/Views/AdminAuth/Login.cshtml", vm);
        }

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTimeOffset.UtcNow)
        {
            ModelState.AddModelError("", "Account temporarily locked. Try again later.");
            return View("~/Views/AdminAuth/Login.cshtml", vm);
        }

        if (!_hasher.Verify(user.PasswordHash, vm.Password))
        {
            var newCount = user.FailedLoginCount + 1;
            DateTimeOffset? lockUntil = newCount >= _opt.MaxFailedAttempts
                ? DateTimeOffset.UtcNow.AddMinutes(_opt.LockoutMinutes)
                : null;
            await _users.UpdateLoginFailureAsync(user.Id, newCount, lockUntil);
            ModelState.AddModelError("", "Invalid credentials.");
            return View("~/Views/AdminAuth/Login.cshtml", vm);
        }

        await _users.UpdateLoginSuccessAsync(user.Id);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("role", "SuperAdmin")
        };
        var identity = new ClaimsIdentity(claims, "Admin");
        await HttpContext.SignInAsync("Admin", new ClaimsPrincipal(identity));

        await _audit.WriteGlobalAsync("admin.login", "global_users", user.Id.ToString(), null);

        return Redirect(string.IsNullOrWhiteSpace(vm.ReturnUrl) ? "/admin" : vm.ReturnUrl);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Admin");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("denied")]
    public IActionResult Denied() => View("~/Views/AdminAuth/Denied.cshtml");
}
