using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alignd.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Alignd.Infrastructure.Auth;

public sealed class AdminTokenService(IConfiguration config) : IAdminTokenService
{
    private string Secret => config["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

    public string Generate(Guid roomId)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims  = new[] {
            new Claim("roomId",  roomId.ToString()),
            new Claim("isAdmin", "true")
        };

        var token = new JwtSecurityToken(
            claims:  claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool Validate(string token, Guid roomId)
    {
        try
        {
            var key       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
            var handler   = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ClockSkew                = TimeSpan.Zero
            }, out _);

            var roomClaim    = principal.FindFirst("roomId")?.Value;
            var isAdminClaim = principal.FindFirst("isAdmin")?.Value;

            return roomClaim == roomId.ToString() && isAdminClaim == "true";
        }
        catch { return false; }
    }
}
