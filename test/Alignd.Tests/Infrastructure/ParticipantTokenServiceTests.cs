using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alignd.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Alignd.Tests.Infrastructure;

/// <summary>
/// Tests for <see cref="ParticipantTokenService"/>.
/// Uses a real in-memory IConfiguration to keep the tests fast and self-contained.
/// </summary>
[TestFixture]
public sealed class ParticipantTokenServiceTests
{
    private const string TestSecret = "participant-secret-key-long-enough-for-hmac-sha256";

    private ParticipantTokenService CreateService(string? secret = TestSecret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret
            })
            .Build();

        return new ParticipantTokenService(config);
    }

    [Test]
    public void Generate_ReturnsNonEmptyToken()
    {
        var service = CreateService();

        var token = service.Generate(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Generate_ReturnsDifferentTokens_ForDifferentParticipants()
    {
        var service = CreateService();
        var roomId  = Guid.NewGuid();

        var token1 = service.Generate(Guid.NewGuid(), roomId);
        var token2 = service.Generate(Guid.NewGuid(), roomId);

        Assert.That(token1, Is.Not.EqualTo(token2),
            "Different participants must receive different tokens");
    }

    [Test]
    public void Generate_ThenValidate_ReturnsBothIds_Correctly()
    {
        var service       = CreateService();
        var participantId = Guid.NewGuid();
        var roomId        = Guid.NewGuid();

        var token  = service.Generate(participantId, roomId);
        var result = service.Validate(token);

        Assert.That(result, Is.Not.Null,
            "A freshly generated token must validate successfully");
        Assert.Multiple(() =>
        {
            Assert.That(result!.Value.participantId, Is.EqualTo(participantId),
                "The participantId claim must round-trip through the token");
            Assert.That(result!.Value.roomId,        Is.EqualTo(roomId),
                "The roomId claim must round-trip through the token");
        });
    }

    [Test]
    public void Validate_WithTamperedToken_ReturnsNull()
    {
        var service = CreateService();

        var result = service.Validate("tampered.jwt.token");

        Assert.That(result, Is.Null,
            "A tampered or malformed token must return null");
    }

    [Test]
    public void Validate_WithEmptyToken_ReturnsNull()
    {
        var service = CreateService();

        var result = service.Validate(string.Empty);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Validate_WithTokenSignedByDifferentSecret_ReturnsNull()
    {
        var serviceA = CreateService("first-secret-key-long-enough-for-hmac-sha256-algorithm");
        var serviceB = CreateService("other-secret-key-long-enough-for-hmac-sha256-algorithm");
        var token    = serviceA.Generate(Guid.NewGuid(), Guid.NewGuid());

        var result = serviceB.Validate(token);

        Assert.That(result, Is.Null,
            "A token signed with a different secret must not validate");
    }

    [Test]
    public void Validate_WithValidlySignedToken_ButClaimsAreNotGuids_ReturnsNull()
    {
        var service = CreateService();

        // Build a token signed with the correct secret whose participantId
        // and roomId claims are not valid GUIDs — exercises the TryParse fallback.
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim("participantId", "not-a-guid"),
                new Claim("roomId",        "also-not-a-guid")
            },
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var result = service.Validate(tokenString);

        Assert.That(result, Is.Null,
            "A validly signed token whose claims cannot be parsed as GUIDs must return null");
    }

    [Test]
    public void Generate_WhenJwtSecretIsMissing_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var service = new ParticipantTokenService(config);

        Assert.Throws<InvalidOperationException>(
            () => service.Generate(Guid.NewGuid(), Guid.NewGuid()),
            "When Jwt:Secret is not configured, the service must throw an InvalidOperationException");
    }

    [Test]
    public void Validate_WithValidToken_ParticipantIdIsNotEmpty()
    {
        var service = CreateService();
        var token   = service.Generate(Guid.NewGuid(), Guid.NewGuid());

        var result = service.Validate(token);

        Assert.That(result!.Value.participantId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Validate_WithValidToken_RoomIdIsNotEmpty()
    {
        var service = CreateService();
        var token   = service.Generate(Guid.NewGuid(), Guid.NewGuid());

        var result = service.Validate(token);

        Assert.That(result!.Value.roomId, Is.Not.EqualTo(Guid.Empty));
    }
}
