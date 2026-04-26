using Appsdemo.TravelCrm.Core.Multitenancy;
using Appsdemo.TravelCrm.Data.Repositories.Master;
using Appsdemo.TravelCrm.Data.Security;
using Appsdemo.TravelCrm.Web.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Appsdemo.TravelCrm.Web.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenancyOptions _opt;
    private readonly ILogger<TenantResolutionMiddleware> _log;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        IOptions<TenancyOptions> opt,
        ILogger<TenantResolutionMiddleware> log)
    {
        _next = next;
        _opt = opt.Value;
        _log = log;
    }

    public async Task InvokeAsync(
        HttpContext ctx,
        ITenantContextAccessor accessor,
        ITenantRepository tenants,
        ISecretCipher cipher,
        IMemoryCache cache)
    {
        var path = ctx.Request.Path.Value ?? "";

        // Always-public paths: never need tenant
        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/css/")  || path.StartsWith("/js/")
            || path.StartsWith("/lib/")  || path.StartsWith("/uploads/")
            || path.StartsWith("/favicon"))
        {
            await _next(ctx);
            return;
        }

        var code = ResolveTenantCode(ctx, path, out var pathPrefix);
        if (string.IsNullOrWhiteSpace(code))
        {
            await _next(ctx);
            return;
        }

        var tenant = await cache.GetOrCreateAsync($"tenant:{code}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var t = await tenants.GetByCodeAsync(code);
            if (t is null) return null;
            var features = await tenants.GetFeaturesForPlanAsync(t.PlanId);

            string password;
            try { password = cipher.Decrypt(t.DbPasswordEncrypted); }
            catch { password = t.DbPasswordEncrypted; }

            var connStr = $"Host={t.DbHost};Port={t.DbPort};Database={t.DbName};Username={t.DbUser};Password={password};Pooling=true;Maximum Pool Size=200";
            return new TenantContext
            {
                TenantId = t.Id,
                Code = t.Code,
                CompanyName = t.CompanyName,
                ConnectionString = connStr,
                Currency = t.CurrencyCode,
                Timezone = t.Timezone,
                PlanId = t.PlanId,
                Features = features
            };
        });

        if (tenant is null)
        {
            _log.LogWarning("No tenant found for code '{Code}'", code);
            ctx.Response.StatusCode = 404;
            await ctx.Response.WriteAsync($"Unknown tenant: {code}");
            return;
        }

        accessor.Current = tenant;
        ctx.Items["Tenant"] = tenant;

        if (pathPrefix.HasValue)
        {
            var originalPath = ctx.Request.Path;
            var originalPathBase = ctx.Request.PathBase;
            ctx.Request.PathBase = originalPathBase.Add(pathPrefix.Value);
            ctx.Request.Path = originalPath.StartsWithSegments(pathPrefix.Value, out var remaining)
                ? remaining
                : originalPath;

            try
            {
                await _next(ctx);
            }
            finally
            {
                ctx.Request.Path = originalPath;
                ctx.Request.PathBase = originalPathBase;
            }

            return;
        }

        await _next(ctx);
    }

    private string? ResolveTenantCode(HttpContext ctx, string path, out PathString? pathPrefix)
    {
        pathPrefix = null;
        var host = ctx.Request.Host.Host;
        var rootDomain = _opt.RootDomain;

        // Subdomain path: e.g. acme.appsdemo.local
        if (host.EndsWith("." + rootDomain, StringComparison.OrdinalIgnoreCase))
        {
            var sub = host[..^(rootDomain.Length + 1)];
            if (!string.IsNullOrWhiteSpace(sub) &&
                !sub.Equals(_opt.AdminHost, StringComparison.OrdinalIgnoreCase) &&
                !sub.Equals("www", StringComparison.OrdinalIgnoreCase))
            {
                return sub.ToLowerInvariant();
            }
            return null;
        }

        // Path-prefix mode: /t/{code}/...
        if (path.StartsWith("/t/", StringComparison.OrdinalIgnoreCase))
        {
            var rest = path.Substring(3);
            var slash = rest.IndexOf('/');
            var code = slash >= 0 ? rest.Substring(0, slash) : rest;
            if (!string.IsNullOrWhiteSpace(code))
            {
                pathPrefix = new PathString($"/t/{code}");
                return code.ToLowerInvariant();
            }
        }

        // Dev fallback
        if (!string.IsNullOrWhiteSpace(_opt.DefaultDevTenantCode)
            && (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || host.StartsWith("127.0.0.1")))
        {
            return _opt.DefaultDevTenantCode;
        }

        return null;
    }
}

public sealed class IsAdminHostMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenancyOptions _opt;

    public IsAdminHostMiddleware(RequestDelegate next, IOptions<TenancyOptions> opt)
    { _next = next; _opt = opt.Value; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var host = ctx.Request.Host.Host;
        var path = ctx.Request.Path.Value ?? "";
        var isAdminBySubdomain = host.Equals($"{_opt.AdminHost}.{_opt.RootDomain}",
            StringComparison.OrdinalIgnoreCase);
        var isAdminByPath = path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
                            || path.Equals("/hangfire", StringComparison.OrdinalIgnoreCase)
                            || path.StartsWith("/hangfire/", StringComparison.OrdinalIgnoreCase);
        ctx.Items["IsAdminHost"] = isAdminBySubdomain || isAdminByPath;
        await _next(ctx);
    }
}
