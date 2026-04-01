using Alignd.Domain.Entities;

namespace Alignd.Tests.Domain;

[TestFixture]
public sealed class VoteTests
{
    [Test]
    public void Cast_SetsRoundId_Correctly()
    {
        var roundId = Guid.NewGuid();

        var vote = Vote.Cast(roundId, Guid.NewGuid(), "5");

        Assert.That(vote.RoundId, Is.EqualTo(roundId));
    }

    [Test]
    public void Cast_SetsParticipantId_Correctly()
    {
        var participantId = Guid.NewGuid();

        var vote = Vote.Cast(Guid.NewGuid(), participantId, "8");

        Assert.That(vote.ParticipantId, Is.EqualTo(participantId));
    }

    [Test]
    public void Cast_SetsValue_Correctly()
    {
        var vote = Vote.Cast(Guid.NewGuid(), Guid.NewGuid(), "13");

        Assert.That(vote.Value, Is.EqualTo("13"));
    }

    [Test]
    public void Cast_AssignsNewGuid_AsId()
    {
        var vote = Vote.Cast(Guid.NewGuid(), Guid.NewGuid(), "XL");

        Assert.That(vote.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Cast_SetsCastAt_ToARecentUtcTimestamp()
    {
        var before = DateTime.UtcNow;

        var vote = Vote.Cast(Guid.NewGuid(), Guid.NewGuid(), "?");

        Assert.That(vote.CastAt, Is.GreaterThanOrEqualTo(before),
            "CastAt must be set to the current UTC time when the vote is cast");
    }

    [TestCase("1")]
    [TestCase("2")]
    [TestCase("3")]
    [TestCase("5")]
    [TestCase("8")]
    [TestCase("13")]
    [TestCase("21")]
    [TestCase("?")]
    [TestCase("☕")]
    [TestCase("XS")]
    [TestCase("S")]
    [TestCase("M")]
    [TestCase("L")]
    [TestCase("XL")]
    [TestCase("XXL")]
    public void Cast_AcceptsAnyVoteValue_WithoutValidation(string value)
    {
        var vote = Vote.Cast(Guid.NewGuid(), Guid.NewGuid(), value);

        Assert.That(vote.Value, Is.EqualTo(value),
            "Vote.Cast() is a factory method and does not validate the vote value — that is the handler's responsibility");
    }
}
