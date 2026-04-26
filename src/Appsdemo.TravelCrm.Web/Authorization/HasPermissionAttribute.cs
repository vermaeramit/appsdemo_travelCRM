using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Appsdemo.TravelCrm.Web.Authorization;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = $"Perm:{permission}";
    }
    public string Permission { get; }
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, PermissionRequirement req)
    {
        if (ctx.User.Identity?.IsAuthenticated != true) return Task.CompletedTask;

        var has = ctx.User.HasClaim("perm", req.Permission)
                  || ctx.User.HasClaim("perm", "*")
                  || ctx.User.IsInRole("TenantAdmin");
        if (has) ctx.Succeed(req);
        return Task.CompletedTask;
    }
}

public sealed class PermissionPolicyProvider : Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()  => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("Perm:", StringComparison.Ordinal))
        {
            var permission = policyName.Substring(5);
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}
