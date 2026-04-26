namespace Appsdemo.TravelCrm.Web.Navigation;

public sealed class MenuItem
{
    public string Label { get; init; } = "";
    public string Icon { get; init; } = "ti ti-circle";
    public string? Url { get; init; }
    public string? Permission { get; init; }
    public string? Feature { get; init; }
    public string? RouteController { get; init; }
    public string? RoutePathPrefix { get; init; }
    public IReadOnlyList<MenuItem> Children { get; init; } = Array.Empty<MenuItem>();

    public bool IsActive { get; set; }
    public bool IsExpanded { get; set; }
    public bool HasChildren => Children.Count > 0;
}

public sealed class MenuGroup
{
    public string? Title { get; init; }
    public IReadOnlyList<MenuItem> Items { get; init; } = Array.Empty<MenuItem>();
    public bool HasAnyVisible => Items.Count > 0;
}

public enum MenuTreeKind { Tenant, SuperAdmin }
