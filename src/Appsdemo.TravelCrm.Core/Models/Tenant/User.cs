namespace Appsdemo.TravelCrm.Core.Models.Tenant;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? Phone { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ReportsToId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public string? ProfileImage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public sealed class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Permission
{
    public string Key { get; set; } = "";
    public string Module { get; set; } = "";
    public string Action { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class Branch
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gstin { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHeadOffice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
