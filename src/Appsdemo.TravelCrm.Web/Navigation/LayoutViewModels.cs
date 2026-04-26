namespace Appsdemo.TravelCrm.Web.Navigation;

public sealed record SidebarVm(
    IReadOnlyList<MenuGroup> Groups,
    string Brand,
    string BrandHref,
    bool IsAdminHost);

public sealed record TopbarVm(
    string? UserName,
    string? PlanName,
    bool IsAdminHost,
    string TenantBase);
