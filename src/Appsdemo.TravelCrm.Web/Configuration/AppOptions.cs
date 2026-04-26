namespace Appsdemo.TravelCrm.Web.Configuration;

public sealed class TenancyOptions
{
    public string Resolution { get; set; } = "Auto"; // Subdomain / Path / Auto
    public string RootDomain { get; set; } = "appsdemo.local";
    public string AdminHost  { get; set; } = "admin";
    public string DefaultDevTenantCode { get; set; } = "demo";
    public string TenantDbPrefix { get; set; } = "appsdemo_";
    public string TenantDbHost { get; set; } = "localhost";
    public int    TenantDbPort { get; set; } = 5432;
    public string TenantDbUser { get; set; } = "postgres";
    public string TenantDbPassword { get; set; } = "postgres";
}

public sealed class AuthOptions
{
    public string CookieName       { get; set; } = "appsdemo.auth";
    public string CookieAdminName  { get; set; } = "appsdemo.admin";
    public int    ExpiryHours      { get; set; } = 8;
    public int    MaxFailedAttempts { get; set; } = 5;
    public int    LockoutMinutes    { get; set; } = 15;
    public JwtOptions Jwt { get; set; } = new();
}

public sealed class JwtOptions
{
    public string Issuer        { get; set; } = "";
    public string Audience      { get; set; } = "";
    public string SecretKey     { get; set; } = "";
    public int    ExpiryMinutes { get; set; } = 480;
}

public sealed class SuperAdminOptions
{
    public string Email           { get; set; } = "";
    public string FullName        { get; set; } = "";
    public string InitialPassword { get; set; } = "";
}
