using Appsdemo.TravelCrm.Core.Multitenancy;
using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Web.Services;

public interface IAuditLogger
{
    Task WriteAsync(string action, string? entity = null, string? entityId = null, object? payload = null);
    Task WriteGlobalAsync(string action, string? entity = null, string? entityId = null, object? payload = null);
}

public sealed class AuditLogger : IAuditLogger
{
    private readonly IHttpContextAccessor _http;
    private readonly ITenantConnectionFactory _tenantConn;
    private readonly IMasterConnectionFactory _masterConn;
    private readonly ITenantContextAccessor _tenantCtx;

    public AuditLogger(
        IHttpContextAccessor http,
        ITenantConnectionFactory tenantConn,
        IMasterConnectionFactory masterConn,
        ITenantContextAccessor tenantCtx)
    {
        _http = http;
        _tenantConn = tenantConn;
        _masterConn = masterConn;
        _tenantCtx = tenantCtx;
    }

    public async Task WriteAsync(string action, string? entity, string? entityId, object? payload)
    {
        if (_tenantCtx.Current is null) return;
        using var conn = _tenantConn.Open();
        await conn.ExecuteAsync(@"
            INSERT INTO audit_log (actor_id, actor_email, action, entity, entity_id, payload, ip, user_agent)
            VALUES (@actorId, @actorEmail, @action, @entity, @entityId, @payload::jsonb, @ip, @userAgent)",
            new
            {
                actorId = TryGetUserId(),
                actorEmail = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                action,
                entity,
                entityId,
                payload = payload is null ? null : System.Text.Json.JsonSerializer.Serialize(payload),
                ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                userAgent = _http.HttpContext?.Request?.Headers?["User-Agent"].ToString()
            });
    }

    public async Task WriteGlobalAsync(string action, string? entity, string? entityId, object? payload)
    {
        using var conn = _masterConn.Open();
        await conn.ExecuteAsync(@"
            INSERT INTO audit_log_global (actor_id, actor_email, action, entity, entity_id, payload, ip, user_agent)
            VALUES (@actorId, @actorEmail, @action, @entity, @entityId, @payload::jsonb, @ip, @userAgent)",
            new
            {
                actorId = TryGetUserId(),
                actorEmail = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                action,
                entity,
                entityId,
                payload = payload is null ? null : System.Text.Json.JsonSerializer.Serialize(payload),
                ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                userAgent = _http.HttpContext?.Request?.Headers?["User-Agent"].ToString()
            });
    }

    private Guid? TryGetUserId()
    {
        var v = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(v, out var g) ? g : null;
    }
}
