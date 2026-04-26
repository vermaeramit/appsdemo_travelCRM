using System.ComponentModel.DataAnnotations;

namespace Appsdemo.TravelCrm.Web.Models.ViewModels;

public sealed class AdminLoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public sealed class TenantListVm
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalCount { get; set; }
    public IReadOnlyList<Core.Models.Master.Tenant> Items { get; set; }
        = Array.Empty<Core.Models.Master.Tenant>();
}

public sealed class CreateTenantVm
{
    [Required, RegularExpression("^[a-z0-9-]{3,30}$",
        ErrorMessage = "Code must be 3–30 chars: lowercase letters, digits or '-'.")]
    public string Code { get; set; } = "";

    [Required, StringLength(200)] public string CompanyName { get; set; } = "";
    [Required, StringLength(150)] public string ContactPerson { get; set; } = "";
    [Required, EmailAddress]      public string Email { get; set; } = "";
    [StringLength(30)]            public string Phone { get; set; } = "";
    public string Country { get; set; } = "India";
    public string Timezone { get; set; } = "Asia/Kolkata";
    public string CurrencyCode { get; set; } = "INR";

    [Required] public Guid PlanId { get; set; }

    [Required, StringLength(150)] public string AdminUserFullName { get; set; } = "";
    [Required, EmailAddress]      public string AdminUserEmail { get; set; } = "";
    [Required, MinLength(8)]      public string AdminUserPassword { get; set; } = "";

    public IReadOnlyList<Core.Models.Master.SubscriptionPlan> AvailablePlans { get; set; }
        = Array.Empty<Core.Models.Master.SubscriptionPlan>();
}

public sealed class TenantLoginVm
{
    [Required] public string EmailOrUsername { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public string? TenantCode { get; set; }
}

public sealed class DashboardVm
{
    public string CompanyName { get; set; } = "";
    public string PlanName { get; set; } = "";
    public int LeadsThisMonth { get; set; }
    public int QuotesThisMonth { get; set; }
    public int BookingsThisMonth { get; set; }
    public decimal RevenueThisMonth { get; set; }
}

public sealed class UserListVm
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalCount { get; set; }
    public IReadOnlyList<Core.Models.Tenant.User> Items { get; set; }
        = Array.Empty<Core.Models.Tenant.User>();
}

public sealed class RoleListVm
{
    public IReadOnlyList<Core.Models.Tenant.Role> Items { get; set; }
        = Array.Empty<Core.Models.Tenant.Role>();
}
