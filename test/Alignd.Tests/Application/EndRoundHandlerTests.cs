using Alignd.Application.Interfaces;
using Alignd.Application.Voting.EndRound;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;
using Alignd.SharedKernel;
using NSubstitute;

namespace Alignd.Tests.Application;

/// <summary>
/// Tests for <see cref="EndRoundHandler"/>.
/// Uses NSubstitute for all dependency mocks.
/// </summary>
[TestFixture]
public sealed class EndRoundHandlerTests
{
    private IRoomRepository        _rooms        = null!;
    private IVotingRoundRepository _rounds       = null!;
    private IParticipantRepository _participants = null!;
    private IRoomNotifier          _notifier     = null!;
    private EndRoundHandler        _handler      = null!;

    [SetUp]
    public void SetUp()
    {
        _rooms        = Substitute.For<IRoomRepository>();
        _rounds       = Substitute.For<IVotingRoundRepository>();
        _participants = Substitute.For<IParticipantRepository>();
        _notifier     = Substitute.For<IRoomNotifier>();
        _handler      = new EndRoundHandler(_rooms, _rounds, _participants, _notifier);
    }

    private Room CreateRoomWithAdmin(out Guid adminId)
    {
        var room  = Room.Create(RoomCode.From("GOLD-BEAR-20"), VoteType.Fibonacci);
        var admin = Participant.Create(room.Id, "Admin", ParticipantRole.Admin);
        room.AddParticipant(admin);
        room.SetAdmin(admin.Id);
        adminId = admin.Id;
        return room;
    }

    [Test]
    public async Task HandleAsync_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns((Room?)null);

        var result = await _handler.HandleAsync(
            new EndRoundCommand("MISSING", Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task HandleAsync_WhenCallerIsNotTheAdmin_ReturnsForbidden()
    {
        var room = CreateRoomWithAdmin(out _);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);

        var result = await _handler.HandleAsync(
            new EndRoundCommand("GOLD-BEAR-20", Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Forbidden),
            "Only the room admin may manually end a voting round");
    }

    [Test]
    public async Task HandleAsync_WhenNoActiveRoundExists_ReturnsConflict()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>())
               .Returns((VotingRound?)null);

        var result = await _handler.HandleAsync(
            new EndRoundCommand("GOLD-BEAR-20", adminId), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Conflict),
            "Attempting to end a round when none is active must return a Conflict");
    }

    [Test]
    public async Task HandleAsync_WhenActiveRoundExists_EndsTheRound()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>()).Returns(round);
        _participants.GetConnectedByRoomAsync(room.Id, Arg.Any<CancellationToken>())
                     .Returns(new List<Participant>());

        await _handler.HandleAsync(
            new EndRoundCommand("GOLD-BEAR-20", adminId), CancellationToken.None);

        Assert.That(round.Status, Is.EqualTo(RoundStatus.Ended),
            "HandleAsync must transition the round to the Ended status");
    }

    [Test]
    public async Task HandleAsync_WhenActiveRoundExists_SavesChanges()
    {
        var room  = CreateRoomWithAdmin(out var adminId);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>()).Returns(round);
        _participants.GetConnectedByRoomAsync(room.Id, Arg.Any<CancellationToken>())
                     .Returns(new List<Participant>());

        await _handler.HandleAsync(
            new EndRoundCommand("GOLD-BEAR-20", adminId), CancellationToken.None);

        await _rounds.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_OnSuccess_NotifiesRoomThatRoundEnded()
    {
        var room  = CreateRoomWithAdmin(out var adminId);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>()).Returns(round);
        _participants.GetConnectedByRoomAsync(room.Id, Arg.Any<CancellationToken>())
                     .Returns(new List<Participant>());

        await _handler.HandleAsync(
            new EndRoundCommand("GOLD-BEAR-20", adminId), CancellationToken.None);

        await _notifier.Received(1).NotifyRoundEnded(
            Arg.Any<string>(), Arg.Any<RoundEndedPayload>());
    }

    [Test]
    public async Task HandleAsync_WhenVotesExist_IncludesVoteResultsInNotification()
    {
        var room  = CreateRoomWithAdmin(out var adminId);
        var voter = Participant.Create(room.Id, "Voter", ParticipantRole.Voter);
        room.AddParticipant(voter);

        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();
        round.AddVote(Vote.Cast(round.Id, voter.Id, "5"));

        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>()).Returns(round);
        _participants.GetConnectedByRoomAsync(room.Id, Arg.Any<CancellationToken>())
                     .Returns(new List<Participant> { voter });

        RoundEndedPayload? capturedPayload = null;
        await _notifier.NotifyRoundEnded(
            Arg.Any<string>(),
            Arg.Do<RoundEndedPayload>(p => capturedPayload = p));

        await _handler.HandleAsync(
            new EndRoundCommand("GOLD-BEAR-20", adminId), CancellationToken.None);

        Assert.That(capturedPayload, Is.Not.Null);
        Assert.That(capturedPayload!.Votes, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_OnSuccess_ReturnsOkResult()
    {
        var room  = CreateRoomWithAdmin(out var adminId);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>()).Returns(round);
        _participants.GetConnectedByRoomAsync(room.Id, Arg.Any<CancellationToken>())
                     .Returns(new List<Participant>());

        var result = await _handler.HandleAsync(
            new EndRoundCommand("GOLD-BEAR-20", adminId), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Ok));
    }
}
