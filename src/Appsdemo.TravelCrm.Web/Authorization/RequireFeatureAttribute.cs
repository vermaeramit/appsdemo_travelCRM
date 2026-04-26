using Appsdemo.TravelCrm.Core.Multitenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Appsdemo.TravelCrm.Web.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireFeatureAttribute : Attribute, IAsyncActionFilter
{
    public string FeatureKey { get; }
    public RequireFeatureAttribute(string featureKey) => FeatureKey = featureKey;

    public Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var accessor = ctx.HttpContext.RequestServices
            .GetRequiredService<ITenantContextAccessor>();
        var tenant = accessor.Current;

        if (tenant is null || !tenant.HasFeature(FeatureKey))
        {
            ctx.Result = new ViewResult { ViewName = "FeatureLocked" };
            return Task.CompletedTask;
        }
        return next();
    }
}
