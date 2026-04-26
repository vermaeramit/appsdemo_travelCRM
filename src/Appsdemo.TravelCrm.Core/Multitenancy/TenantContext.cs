namespace Appsdemo.TravelCrm.Core.Multitenancy;

public sealed class TenantContext
{
    public Guid TenantId { get; init; }
    public string Code { get; init; } = "";
    public string CompanyName { get; init; } = "";
    public string ConnectionString { get; init; } = "";
    public string Currency { get; init; } = "INR";
    public string Timezone { get; init; } = "Asia/Kolkata";
    public Guid PlanId { get; init; }
    public string PlanName { get; init; } = "";
    public IReadOnlyDictionary<string, string> Features { get; init; } =
        new Dictionary<string, string>();

    public bool HasFeature(string key) =>
        Features.TryGetValue(key, out var v) &&
        !string.IsNullOrWhiteSpace(v) &&
        !v.Equals("false", StringComparison.OrdinalIgnoreCase) &&
        v != "0";
}

public interface ITenantContextAccessor
{
    TenantContext? Current { get; set; }
}
