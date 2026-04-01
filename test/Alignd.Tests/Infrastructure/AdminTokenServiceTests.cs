using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alignd.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Alignd.Tests.Infrastructure;

/// <summary>
/// Tests for <see cref="AdminTokenService"/>.
/// Uses a real in-memory IConfiguration to avoid mocking infrastructure details.
/// </summary>
[TestFixture]
public sealed class AdminTokenServiceTests
{
    private const string TestSecret = "super-secret-key-that-is-long-enough-for-hmac-sha256";

    private AdminTokenService CreateService(string? secret = TestSecret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret
            })
            .Build();

        return new AdminTokenService(config);
    }

    [Test]
    public void Generate_ReturnsNonEmptyToken()
    {
        var service = CreateService();
        var roomId  = Guid.NewGuid();

        var token = service.Generate(roomId);

        Assert.That(token, Is.Not.Null.And.Not.Empty,
            "Generate() must return a non-empty JWT string");
    }

    [Test]
    public void Generate_ReturnsDifferentTokens_ForDifferentRooms()
    {
        var service = CreateService();

        var token1 = service.Generate(Guid.NewGuid());
        var token2 = service.Generate(Guid.NewGuid());

        Assert.That(token1, Is.Not.EqualTo(token2),
            "Each room must get a unique token because the roomId claim differs");
    }

    [Test]
    public void Generate_ThenValidate_WithSameRoomId_ReturnsTrue()
    {
        var service = CreateService();
        var roomId  = Guid.NewGuid();

        var token  = service.Generate(roomId);
        var result = service.Validate(token, roomId);

        Assert.That(result, Is.True,
            "A token generated for a room must validate successfully when the same roomId is provided");
    }

    [Test]
    public void Validate_WithWrongRoomId_ReturnsFalse()
    {
        var service = CreateService();
        var token   = service.Generate(Guid.NewGuid());

        var result = service.Validate(token, Guid.NewGuid());

        Assert.That(result, Is.False,
            "A token must not validate when a different roomId is supplied");
    }

    [Test]
    public void Validate_WithTamperedToken_ReturnsFalse()
    {
        var service = CreateService();
        var roomId  = Guid.NewGuid();

        var result = service.Validate("this.is.not.a.valid.jwt", roomId);

        Assert.That(result, Is.False,
            "An invalid/tampered token must not pass validation");
    }

    [Test]
    public void Validate_WithEmptyToken_ReturnsFalse()
    {
        var service = CreateService();

        var result = service.Validate(string.Empty, Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public void Validate_WithTokenSignedByDifferentSecret_ReturnsFalse()
    {
        var serviceA = CreateService("secret-key-one-long-enough-for-hmac-sha256-algorithm");
        var serviceB = CreateService("secret-key-two-long-enough-for-hmac-sha256-algorithm");
        var roomId   = Guid.NewGuid();

        var token  = serviceA.Generate(roomId);
        var result = serviceB.Validate(token, roomId);

        Assert.That(result, Is.False,
            "A token signed with a different secret must fail validation");
    }

    [Test]
    public void Validate_WithTokenForCorrectRoom_ButIsAdminClaimIsFalse_ReturnsFalse()
    {
        var service = CreateService();
        var roomId  = Guid.NewGuid();

        // Build a validly signed token that carries the correct roomId
        // but explicitly sets isAdmin to "false" instead of "true".
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim("roomId",  roomId.ToString()),
                new Claim("isAdmin", "false")
            },
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var result = service.Validate(tokenString, roomId);

        Assert.That(result, Is.False,
            "A token that carries the correct roomId but does not have isAdmin=true must be rejected");
    }

    [Test]
    public void Generate_WhenJwtSecretIsMissing_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var service = new AdminTokenService(config);

        Assert.Throws<InvalidOperationException>(() => service.Generate(Guid.NewGuid()),
            "When Jwt:Secret is not configured, the service must throw an InvalidOperationException");
    }
}
