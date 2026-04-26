namespace Appsdemo.TravelCrm.Core.Models.Master;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string ContactPerson { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Country { get; set; } = "India";
    public string Timezone { get; set; } = "Asia/Kolkata";
    public string CurrencyCode { get; set; } = "INR";
    public string DbName { get; set; } = "";
    public string DbHost { get; set; } = "localhost";
    public int DbPort { get; set; } = 5432;
    public string DbUser { get; set; } = "";
    public string DbPasswordEncrypted { get; set; } = "";
    public Guid PlanId { get; set; }
    public string Status { get; set; } = "Trial";
    public DateTime? TrialEndsOn { get; set; }
    public DateTime? SubscriptionStartsOn { get; set; }
    public DateTime? SubscriptionEndsOn { get; set; }
    public int MaxUsers { get; set; }
    public string? LogoPath { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}

public sealed class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public int MaxUsers { get; set; }
    public int MaxBranches { get; set; }
    public int TrialDays { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public sealed class GlobalUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
