using System.Text;
using Appsdemo.TravelCrm.Core.Multitenancy;
using Appsdemo.TravelCrm.Data.Connection;
using Appsdemo.TravelCrm.Data.Repositories.Master;
using Appsdemo.TravelCrm.Data.Repositories.Tenant;
using Appsdemo.TravelCrm.Data.Security;
using Appsdemo.TravelCrm.Web.Authorization;
using Appsdemo.TravelCrm.Web.Configuration;
using Appsdemo.TravelCrm.Web.Middleware;
using Appsdemo.TravelCrm.Web.Services;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Local-only overrides: appsettings.Local.json is gitignored.
// Use it for dev DB passwords, secrets, etc. — never commit.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// ---- Logging
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

// ---- Configuration POCOs
builder.Services.Configure<TenancyOptions>(builder.Configuration.GetSection("Tenancy"));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<SuperAdminOptions>(builder.Configuration.GetSection("SuperAdmin"));
var auth = builder.Configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();

// ---- Data Protection (encrypts tenant DB passwords + cookies)
builder.Services.AddDataProtection()
    .SetApplicationName("Appsdemo.TravelCrm")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")));

// ---- Core services
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddSingleton<ISecretCipher, SecretCipher>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// ---- Connection factories + repositories
builder.Services.AddSingleton<IMasterConnectionFactory, MasterConnectionFactory>();
builder.Services.AddScoped<ITenantConnectionFactory, TenantConnectionFactory>();

builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<IGlobalUserRepository, GlobalUserRepository>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

builder.Services.AddScoped<ITenantProvisioner, TenantProvisioner>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IBootstrapper, Bootstrapper>();

// ---- Authentication: cookies (MVC) + JWT (API)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = auth.CookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/auth/logout";
    options.AccessDeniedPath = "/auth/denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(auth.ExpiryHours);
    options.SlidingExpiration = true;
})
.AddCookie("Admin", options =>
{
    options.Cookie.Name = auth.CookieAdminName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/admin/login";
    options.LogoutPath = "/admin/logout";
    options.AccessDeniedPath = "/admin/denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(auth.ExpiryHours);
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = auth.Jwt.Issuer,
        ValidAudience = auth.Jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.Jwt.SecretKey))
    };
});

// ---- Authorization: permission policies + handler
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", p =>
        p.AddAuthenticationSchemes("Admin").RequireAuthenticatedUser());
});

// ---- Hangfire (Postgres storage; created lazily — won't block startup if missing)
builder.Services.AddHangfire(cfg =>
{
    var hf = builder.Configuration.GetConnectionString("HangfirePg");
    if (!string.IsNullOrWhiteSpace(hf))
    {
        cfg.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hf));
    }
});

// ---- MVC + Razor
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// ---- Forwarded headers (when behind a reverse proxy later)
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// ---- One-time bootstrap on startup
using (var scope = app.Services.CreateScope())
{
    var boot = scope.ServiceProvider.GetRequiredService<IBootstrapper>();
    try
    {
        await boot.EnsureMasterAsync();
        await boot.EnsureSuperAdminAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Bootstrap deferred — Postgres may not be running yet. Start Postgres and restart the app.");
    }
}

// ---- Pipeline
app.UseForwardedHeaders();
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
app.UseMiddleware<IsAdminHostMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard for super-admins only
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new SuperAdminHangfireFilter() }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.MapGet("/_health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

app.Run();

internal sealed class SuperAdminHangfireFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User?.Identity?.IsAuthenticated == true
               && (http.User.Identity?.AuthenticationType == "Admin"
                   || http.User.HasClaim("role", "SuperAdmin"));
    }
}
