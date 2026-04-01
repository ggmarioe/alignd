using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alignd.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Alignd.Infrastructure.Auth;

public sealed class ParticipantTokenService(IConfiguration config) : IParticipantTokenService
{
    private string Secret => config["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

    public string Generate(Guid participantId, Guid roomId)
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] {
            new Claim("participantId", participantId.ToString()),
            new Claim("roomId",        roomId.ToString())
        };

        var token = new JwtSecurityToken(
            claims:  claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (Guid participantId, Guid roomId)? Validate(string token)
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

            var pId = principal.FindFirst("participantId")?.Value;
            var rId = principal.FindFirst("roomId")?.Value;

            if (Guid.TryParse(pId, out var participantId) &&
                Guid.TryParse(rId, out var roomId))
                return (participantId, roomId);

            return null;
        }
        catch { return null; }
    }
}
