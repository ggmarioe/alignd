using Alignd.Domain.Entities;
using Alignd.Domain.Enums;

namespace Alignd.Tests.Domain;

[TestFixture]
public sealed class VotingRoundTests
{
    private static readonly Guid RoomId = Guid.NewGuid();
    private static readonly Guid TaskId = Guid.NewGuid();

    [Test]
    public void CreateForTask_SetsRoomId_Correctly()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);

        Assert.That(round.RoomId, Is.EqualTo(RoomId));
    }

    [Test]
    public void CreateForTask_SetsTaskItemId_Correctly()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);

        Assert.That(round.TaskItemId, Is.EqualTo(TaskId));
    }

    [Test]
    public void CreateForTask_InitialStatus_IsPending()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);

        Assert.That(round.Status, Is.EqualTo(RoundStatus.Pending),
            "A newly created round must start in Pending status");
    }

    [Test]
    public void CreateForTask_FreeTitle_IsNull()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);

        Assert.That(round.FreeTitle, Is.Null,
            "A task-based round must not have a free title");
    }

    [Test]
    public void CreateFree_SetsFreeTitle_Correctly()
    {
        var round = VotingRound.CreateFree(RoomId, "Sprint planning");

        Assert.That(round.FreeTitle, Is.EqualTo("Sprint planning"));
    }

    [Test]
    public void CreateFree_TaskItemId_IsNull()
    {
        var round = VotingRound.CreateFree(RoomId, "Any title");

        Assert.That(round.TaskItemId, Is.Null,
            "A free round must not be associated with a task");
    }

    [Test]
    public void Start_ChangesStatus_ToActive()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);

        round.Start();

        Assert.That(round.Status, Is.EqualTo(RoundStatus.Active));
    }

    [Test]
    public void Start_SetsStartedAt_ToANonNullTimestamp()
    {
        var before = DateTime.UtcNow;
        var round  = VotingRound.CreateForTask(RoomId, TaskId);

        round.Start();

        Assert.That(round.StartedAt, Is.Not.Null);
        Assert.That(round.StartedAt, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void End_ChangesStatus_ToEnded()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);
        round.Start();

        round.End();

        Assert.That(round.Status, Is.EqualTo(RoundStatus.Ended));
    }

    [Test]
    public void End_SetsEndedAt_ToANonNullTimestamp()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);
        round.Start();
        var before = DateTime.UtcNow;

        round.End();

        Assert.That(round.EndedAt, Is.Not.Null);
        Assert.That(round.EndedAt, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void AddVote_IncreasesVoteCount_ByOne()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);
        round.Start();
        var vote = Vote.Cast(round.Id, Guid.NewGuid(), "5");

        round.AddVote(vote);

        Assert.That(round.Votes, Has.Count.EqualTo(1));
    }

    [Test]
    public void TopVotes_WithNoVotes_ReturnsEmptyList()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);

        var top = round.TopVotes();

        Assert.That(top, Is.Empty,
            "TopVotes() on a round with zero votes must return an empty collection");
    }

    [Test]
    public void TopVotes_WhenAllVotersAgree_ReturnsThatSingleValue()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);
        round.AddVote(Vote.Cast(round.Id, Guid.NewGuid(), "5"));
        round.AddVote(Vote.Cast(round.Id, Guid.NewGuid(), "5"));
        round.AddVote(Vote.Cast(round.Id, Guid.NewGuid(), "5"));

        var top = round.TopVotes();

        Assert.That(top, Is.EquivalentTo(new[] { "5" }),
            "When every voter picks the same value the top vote must be that value");
    }

    [Test]
    public void TopVotes_WhenVotesAreSplit_ReturnsBothTiedValues()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);
        round.AddVote(Vote.Cast(round.Id, Guid.NewGuid(), "3"));
        round.AddVote(Vote.Cast(round.Id, Guid.NewGuid(), "3"));
        round.AddVote(Vote.Cast(round.Id, Guid.NewGuid(), "5"));
        round.AddVote(Vote.Cast(round.Id, Guid.NewGuid(), "5"));

        var top = round.TopVotes();

        Assert.That(top, Is.EquivalentTo(new[] { "3", "5" }),
            "Both values with the highest equal count must appear in TopVotes()");
    }

    [Test]
    public void TopVotes_WhenOneClearWinner_ReturnsOnlyThatValue()
    {
        var round = VotingRound.CreateForTask(RoomId, TaskId);
        round.AddVote(Vote.Cast(round.Id, Guid.NewGuid(), "8"));
        round.AddVote(Vote.Cast(round.Id, Guid.NewGuid(), "8"));
        round.AddVote(Vote.Cast(round.Id, Guid.NewGuid(), "13"));

        var top = round.TopVotes();

        Assert.That(top, Is.EquivalentTo(new[] { "8" }),
            "Only the most-voted value must be returned when there is a clear winner");
    }
}
