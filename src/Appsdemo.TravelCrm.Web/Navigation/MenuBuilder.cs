using System.Security.Claims;
using Appsdemo.TravelCrm.Core.Multitenancy;
using Appsdemo.TravelCrm.Core.Security;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Appsdemo.TravelCrm.Web.Navigation;

public interface IMenuBuilder
{
    IReadOnlyList<MenuGroup> Build(ViewContext vc, MenuTreeKind kind);
}

public sealed class MenuBuilder : IMenuBuilder
{
    private readonly ITenantContextAccessor _tenant;
    public MenuBuilder(ITenantContextAccessor tenant) => _tenant = tenant;

    public IReadOnlyList<MenuGroup> Build(ViewContext vc, MenuTreeKind kind)
    {
        var raw = kind == MenuTreeKind.SuperAdmin ? BuildSuperAdmin() : BuildTenant();
        var user = vc.HttpContext.User;
        var tenant = _tenant.Current;
        var currentController = vc.RouteData.Values["controller"]?.ToString();
        var currentPath = vc.HttpContext.Request.Path.Value ?? "";
        var pathBase = vc.HttpContext.Request.PathBase.Value ?? "";

        return raw
            .Select(g => new MenuGroup
            {
                Title = g.Title,
                Items = Filter(g.Items, user, tenant, kind, currentController, currentPath, pathBase)
            })
            .Where(g => g.HasAnyVisible)
            .ToList();
    }

    private static IReadOnlyList<MenuItem> Filter(
        IReadOnlyList<MenuItem> items,
        ClaimsPrincipal user,
        TenantContext? tenant,
        MenuTreeKind kind,
        string? currentController,
        string currentPath,
        string pathBase)
    {
        var visible = new List<MenuItem>();
        foreach (var src in items)
        {
            if (!IsVisible(src, user, tenant, kind)) continue;

            var item = new MenuItem
            {
                Label = src.Label,
                Icon = src.Icon,
                Url = ResolveUrl(src.Url, pathBase, kind),
                Permission = src.Permission,
                Feature = src.Feature,
                RouteController = src.RouteController,
                RoutePathPrefix = src.RoutePathPrefix,
                Children = src.HasChildren
                    ? Filter(src.Children, user, tenant, kind, currentController, currentPath, pathBase)
                    : Array.Empty<MenuItem>()
            };

            if (src.HasChildren && item.Children.Count == 0) continue;

            item.IsActive = IsActive(src, currentController, currentPath, pathBase)
                            || item.Children.Any(c => c.IsActive);
            item.IsExpanded = item.HasChildren && item.Children.Any(c => c.IsActive);
            visible.Add(item);
        }
        return visible;
    }

    private static string? ResolveUrl(string? url, string pathBase, MenuTreeKind kind)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return url;
        if (kind == MenuTreeKind.SuperAdmin) return url;
        if (string.IsNullOrEmpty(pathBase)) return url;
        if (url == "/") return pathBase + "/";
        return pathBase + url;
    }

    private static bool IsVisible(MenuItem item, ClaimsPrincipal user, TenantContext? tenant, MenuTreeKind kind)
    {
        if (kind == MenuTreeKind.Tenant)
        {
            if (!string.IsNullOrEmpty(item.Feature)
                && (tenant is null || !tenant.HasFeature(item.Feature)))
                return false;

            if (!string.IsNullOrEmpty(item.Permission))
            {
                var ok = user.HasClaim("perm", item.Permission)
                         || user.HasClaim("perm", "*")
                         || user.IsInRole(SystemRoles.TenantAdmin);
                if (!ok) return false;
            }
        }
        return true;
    }

    private static bool IsActive(MenuItem item, string? currentController, string currentPath, string pathBase)
    {
        if (!string.IsNullOrEmpty(item.RouteController) &&
            string.Equals(item.RouteController, currentController, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrEmpty(item.RoutePathPrefix))
        {
            var prefix = pathBase + item.RoutePathPrefix;
            if (currentPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        }

        if (!string.IsNullOrEmpty(item.Url) && item.Url == "/" )
        {
            var fullPath = pathBase + "/";
            if (string.Equals(currentPath, "/", StringComparison.Ordinal)
                || string.Equals(currentPath.TrimEnd('/'), pathBase.TrimEnd('/'), StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    // ---------------------------------------------------------------------
    // Menu definitions — declarative trees
    // ---------------------------------------------------------------------

    private static IReadOnlyList<MenuGroup> BuildTenant() => new[]
    {
        new MenuGroup
        {
            Items = new[]
            {
                new MenuItem
                {
                    Label = "Dashboard",
                    Icon = "ti ti-dashboard",
                    Url = "/",
                    Permission = Permissions.Dashboard.View,
                    RouteController = "Dashboard"
                }
            }
        },
        new MenuGroup
        {
            Title = "Sales",
            Items = new[]
            {
                new MenuItem
                {
                    Label = "Leads", Icon = "ti ti-target-arrow",
                    Url = "/leads", Permission = Permissions.Leads.View,
                    Feature = Features.ModuleCrm, RouteController = "Leads"
                },
                new MenuItem
                {
                    Label = "Quotes", Icon = "ti ti-file-invoice",
                    Url = "/quotes", Permission = Permissions.Quotes.View,
                    Feature = Features.ModuleQuotes, RouteController = "Quotes"
                }
            }
        },
        new MenuGroup
        {
            Title = "Operations",
            Items = new[]
            {
                new MenuItem
                {
                    Label = "Bookings", Icon = "ti ti-calendar-event",
                    Url = "/bookings", Permission = Permissions.Bookings.View,
                    Feature = Features.ModuleBookings, RouteController = "Bookings"
                },
                new MenuItem
                {
                    Label = "Vouchers", Icon = "ti ti-ticket",
                    Url = "/vouchers", Permission = Permissions.Vouchers.View,
                    Feature = Features.ModuleVouchers, RouteController = "Vouchers"
                }
            }
        },
        new MenuGroup
        {
            Title = "Finance",
            Items = new[]
            {
                new MenuItem
                {
                    Label = "Invoices", Icon = "ti ti-receipt",
                    Url = "/invoices", Permission = Permissions.Invoices.View,
                    Feature = Features.ModuleInvoices, RouteController = "Invoices"
                },
                new MenuItem
                {
                    Label = "Payments", Icon = "ti ti-cash",
                    Url = "/payments", Permission = Permissions.Payments.View,
                    Feature = Features.ModulePayments, RouteController = "Payments"
                }
            }
        },
        new MenuGroup
        {
            Title = "Masters",
            Items = new[]
            {
                new MenuItem
                {
                    Label = "Master Data", Icon = "ti ti-database",
                    Permission = Permissions.Masters.View,
                    RoutePathPrefix = "/masters",
                    Children = new[]
                    {
                        new MenuItem { Label = "Destinations",   Icon = "ti ti-map-pin",        Url = "/masters/destinations",   RouteController = "Destinations" },
                        new MenuItem { Label = "Hotels",         Icon = "ti ti-building",       Url = "/masters/hotels",         RouteController = "Hotels" },
                        new MenuItem { Label = "Room Types",     Icon = "ti ti-bed",            Url = "/masters/room-types",     RouteController = "RoomTypes" },
                        new MenuItem { Label = "Sightseeing",    Icon = "ti ti-camera",         Url = "/masters/sightseeing",    RouteController = "Sightseeing" },
                        new MenuItem { Label = "Transport",      Icon = "ti ti-car",            Url = "/masters/transport",      RouteController = "Transport" },
                        new MenuItem { Label = "Suppliers",      Icon = "ti ti-truck-delivery", Url = "/masters/suppliers",      RouteController = "Suppliers" },
                        new MenuItem { Label = "Services",       Icon = "ti ti-tag",            Url = "/masters/services",       RouteController = "Services" },
                        new MenuItem { Label = "Tax Rates",      Icon = "ti ti-percentage",     Url = "/masters/tax-rates",      RouteController = "TaxRates" },
                        new MenuItem { Label = "Currencies",     Icon = "ti ti-currency-dollar",Url = "/masters/currencies",     RouteController = "Currencies" }
                    }
                }
            }
        },
        new MenuGroup
        {
            Title = "Reports",
            Items = new[]
            {
                new MenuItem
                {
                    Label = "Reports", Icon = "ti ti-chart-bar",
                    Url = "/reports", Permission = Permissions.Reports.View,
                    Feature = Features.ModuleReports, RouteController = "Reports"
                }
            }
        },
        new MenuGroup
        {
            Title = "Administration",
            Items = new[]
            {
                new MenuItem
                {
                    Label = "Administration", Icon = "ti ti-settings",
                    RoutePathPrefix = "/admin-tenant",
                    Children = new[]
                    {
                        new MenuItem { Label = "Users",       Icon = "ti ti-users",        Url = "/users",      Permission = Permissions.Users.View,    RouteController = "Users" },
                        new MenuItem { Label = "Roles",       Icon = "ti ti-shield-lock",  Url = "/roles",      Permission = Permissions.Roles.View,    RouteController = "Roles" },
                        new MenuItem { Label = "Branches",    Icon = "ti ti-building-bank",Url = "/branches",   Permission = Permissions.Branches.View, RouteController = "Branches" },
                        new MenuItem { Label = "Settings",    Icon = "ti ti-adjustments",  Url = "/settings",   Permission = Permissions.Settings.View, RouteController = "Settings" },
                        new MenuItem { Label = "Audit Log",   Icon = "ti ti-history",      Url = "/audit",      Permission = Permissions.Audit.View,    RouteController = "Audit" }
                    }
                }
            }
        }
    };

    private static IReadOnlyList<MenuGroup> BuildSuperAdmin() => new[]
    {
        new MenuGroup
        {
            Items = new[]
            {
                new MenuItem { Label = "Tenants",     Icon = "ti ti-building",      Url = "/admin",             RouteController = "Admin" },
                new MenuItem { Label = "New Tenant",  Icon = "ti ti-plus",          Url = "/admin/tenants/new", RoutePathPrefix = "/admin/tenants/new" }
            }
        },
        new MenuGroup
        {
            Title = "Catalog",
            Items = new[]
            {
                new MenuItem { Label = "Subscription Plans", Icon = "ti ti-package",        Url = "/admin/plans",    RouteController = "Plans" },
                new MenuItem { Label = "Features",           Icon = "ti ti-checkbox",       Url = "/admin/features", RouteController = "Features" }
            }
        },
        new MenuGroup
        {
            Title = "Billing",
            Items = new[]
            {
                new MenuItem { Label = "Tenant Invoices",    Icon = "ti ti-receipt-tax",    Url = "/admin/billing",  RouteController = "Billing" }
            }
        },
        new MenuGroup
        {
            Title = "System",
            Items = new[]
            {
                new MenuItem { Label = "Background Jobs",    Icon = "ti ti-clock",          Url = "/hangfire" },
                new MenuItem { Label = "Global Audit Log",   Icon = "ti ti-history",        Url = "/admin/audit-global", RouteController = "GlobalAudit" }
            }
        }
    };
}
