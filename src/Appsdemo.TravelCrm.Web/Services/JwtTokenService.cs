using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Appsdemo.TravelCrm.Web.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Appsdemo.TravelCrm.Web.Services;

public interface IJwtTokenService
{
    string Issue(Guid userId, string email, string tenantCode, IEnumerable<string> permissions);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opt;
    public JwtTokenService(IOptions<AuthOptions> opt) => _opt = opt.Value.Jwt;

    public string Issue(Guid userId, string email, string tenantCode, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("tenant", tenantCode)
        };
        claims.AddRange(permissions.Select(p => new Claim("perm", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.ExpiryMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
