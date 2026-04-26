using System.Security.Claims;
using Appsdemo.TravelCrm.Core.Multitenancy;
using Appsdemo.TravelCrm.Data.Repositories.Tenant;
using Appsdemo.TravelCrm.Web.Configuration;
using Appsdemo.TravelCrm.Web.Models.ViewModels;
using Appsdemo.TravelCrm.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Appsdemo.TravelCrm.Web.Controllers.Auth;

[Route("auth")]
public sealed class AuthController : Controller
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITenantContextAccessor _tenant;
    private readonly AuthOptions _opt;
    private readonly IAuditLogger _audit;

    public AuthController(
        IUserRepository users,
        IPasswordHasher hasher,
        ITenantContextAccessor tenant,
        IOptions<AuthOptions> opt,
        IAuditLogger audit)
    {
        _users = users;
        _hasher = hasher;
        _tenant = tenant;
        _opt = opt.Value;
        _audit = audit;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_tenant.Current is null)
            return Content("No tenant resolved for this URL. Use a valid tenant subdomain or /t/{code}/...");

        return View("~/Views/Auth/Login.cshtml", new TenantLoginVm
        {
            ReturnUrl = returnUrl,
            TenantCode = _tenant.Current.Code
        });
    }

    [HttpPost("login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(TenantLoginVm vm)
    {
        if (_tenant.Current is null)
            return Content("No tenant resolved for this URL.");

        if (!ModelState.IsValid) return View("~/Views/Auth/Login.cshtml", vm);

        var user = await _users.GetByEmailOrUsernameAsync(vm.EmailOrUsername);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View("~/Views/Auth/Login.cshtml", vm);
        }

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTimeOffset.UtcNow)
        {
            ModelState.AddModelError("", "Account temporarily locked. Try again later.");
            return View("~/Views/Auth/Login.cshtml", vm);
        }

        if (!_hasher.Verify(user.PasswordHash, vm.Password))
        {
            var newCount = user.FailedLoginCount + 1;
            DateTimeOffset? lockUntil = newCount >= _opt.MaxFailedAttempts
                ? DateTimeOffset.UtcNow.AddMinutes(_opt.LockoutMinutes)
                : null;
            await _users.UpdateLoginFailureAsync(user.Id, newCount, lockUntil);
            ModelState.AddModelError("", "Invalid credentials.");
            return View("~/Views/Auth/Login.cshtml", vm);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        await _users.UpdateLoginSuccessAsync(user.Id, ip);

        var perms = await _users.GetPermissionsAsync(user.Id);
        var roles = await _users.GetRolesAsync(user.Id);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("tenant", _tenant.Current.Code),
            new("tenant_id", _tenant.Current.TenantId.ToString())
        };
        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r.Name));
        foreach (var p in perms) claims.Add(new Claim("perm", p));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        await _audit.WriteAsync("user.login", "users", user.Id.ToString(), null);

        return Redirect(string.IsNullOrWhiteSpace(vm.ReturnUrl) ? "/" : vm.ReturnUrl);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("denied")]
    public IActionResult Denied() => View("~/Views/Auth/Denied.cshtml");
}
