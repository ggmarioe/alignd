using Alignd.Application.Interfaces;
using Alignd.Application.Participants.Disconnect;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;
using NSubstitute;

namespace Alignd.Tests.Application;

/// <summary>
/// Tests for <see cref="DisconnectHandler"/>.
/// Uses NSubstitute for all dependency mocks.
/// </summary>
[TestFixture]
public sealed class DisconnectHandlerTests
{
    private IRoomRepository        _rooms        = null!;
    private IParticipantRepository _participants = null!;
    private IVotingRoundRepository _rounds       = null!;
    private IRoomNotifier          _notifier     = null!;
    private DisconnectHandler      _handler      = null!;

    [SetUp]
    public void SetUp()
    {
        _rooms        = Substitute.For<IRoomRepository>();
        _participants = Substitute.For<IParticipantRepository>();
        _rounds       = Substitute.For<IVotingRoundRepository>();
        _notifier     = Substitute.For<IRoomNotifier>();
        _handler      = new DisconnectHandler(_rooms, _participants, _rounds, _notifier);
    }

    [Test]
    public async Task HandleAsync_WhenParticipantDoesNotExist_DoesNothingAndReturnsGracefully()
    {
        _participants.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                     .Returns((Participant?)null);

        // Must not throw
        await _handler.HandleAsync(
            new DisconnectCommand(Guid.NewGuid(), "conn-id"), CancellationToken.None);

        await _participants.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_WhenRoomDoesNotExist_DoesNothingAndReturnsGracefully()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Alice", ParticipantRole.Voter);
        _participants.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                     .Returns(participant);
        _rooms.GetByIdAsync(participant.RoomId, Arg.Any<CancellationToken>())
              .Returns((Room?)null);

        await _handler.HandleAsync(
            new DisconnectCommand(participant.Id, "conn-id"), CancellationToken.None);

        await _participants.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_MarksParticipant_AsDisconnected()
    {
        var room        = Room.Create(RoomCode.From("SWIFT-TIGER-42"), VoteType.Fibonacci);
        var participant = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        participant.ConnectionId = "old-conn";
        participant.IsConnected  = true;

        _participants.GetByIdAsync(participant.Id, Arg.Any<CancellationToken>()).Returns(participant);
        _rooms.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>())
               .Returns((VotingRound?)null);

        await _handler.HandleAsync(
            new DisconnectCommand(participant.Id, "old-conn"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(participant.IsConnected,  Is.False,
                "Participant must be marked as disconnected");
            Assert.That(participant.ConnectionId, Is.Null,
                "ConnectionId must be cleared on disconnect");
        });
    }

    [Test]
    public async Task HandleAsync_WhenActiveRoundExists_AndVoterHasNotVoted_CastsAbstainVote()
    {
        var room        = Room.Create(RoomCode.From("SWIFT-TIGER-42"), VoteType.Fibonacci);
        var participant = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        var round       = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();

        _participants.GetByIdAsync(participant.Id, Arg.Any<CancellationToken>()).Returns(participant);
        _rooms.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>()).Returns(round);

        await _handler.HandleAsync(
            new DisconnectCommand(participant.Id, "conn"), CancellationToken.None);

        Assert.That(round.Votes, Has.Count.EqualTo(1),
            "A voter who disconnects without voting must have an abstain ('?') vote cast on their behalf");
        Assert.That(round.Votes[0].Value, Is.EqualTo("?"));
    }

    [Test]
    public async Task HandleAsync_WhenActiveRoundExists_AndVoterAlreadyVoted_DoesNotCastDuplicateVote()
    {
        var room        = Room.Create(RoomCode.From("SWIFT-TIGER-42"), VoteType.Fibonacci);
        var participant = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        var round       = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();
        round.AddVote(Vote.Cast(round.Id, participant.Id, "5")); // already voted

        _participants.GetByIdAsync(participant.Id, Arg.Any<CancellationToken>()).Returns(participant);
        _rooms.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>()).Returns(round);

        await _handler.HandleAsync(
            new DisconnectCommand(participant.Id, "conn"), CancellationToken.None);

        Assert.That(round.Votes, Has.Count.EqualTo(1),
            "No additional vote must be cast if the participant already voted");
    }

    [Test]
    public async Task HandleAsync_WhenActiveRoundExists_AndParticipantIsAWatcher_DoesNotCastAbstain()
    {
        var room        = Room.Create(RoomCode.From("SWIFT-TIGER-42"), VoteType.Fibonacci);
        var watcher     = Participant.Create(room.Id, "Observer", ParticipantRole.Watcher);
        var round       = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();

        _participants.GetByIdAsync(watcher.Id, Arg.Any<CancellationToken>()).Returns(watcher);
        _rooms.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>()).Returns(round);

        await _handler.HandleAsync(
            new DisconnectCommand(watcher.Id, "conn"), CancellationToken.None);

        Assert.That(round.Votes, Is.Empty,
            "Watchers must never have an abstain vote cast for them");
    }

    [Test]
    public async Task HandleAsync_WhenDisconnectedParticipantIsAdmin_NotifiesAdminChange()
    {
        var room  = Room.Create(RoomCode.From("SWIFT-TIGER-42"), VoteType.Fibonacci);
        var admin = Participant.Create(room.Id, "Admin", ParticipantRole.Admin);
        room.AddParticipant(admin);
        room.SetAdmin(admin.Id);

        _participants.GetByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);
        _rooms.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>())
               .Returns((VotingRound?)null);

        await _handler.HandleAsync(
            new DisconnectCommand(admin.Id, "conn"), CancellationToken.None);

        await _notifier.Received(1).NotifyAdminChanged(
            Arg.Any<string>(), Arg.Any<AdminChangedPayload>());
    }

    [Test]
    public async Task HandleAsync_NotifiesRoomThatUserLeft()
    {
        var room        = Room.Create(RoomCode.From("SWIFT-TIGER-42"), VoteType.Fibonacci);
        var participant = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);

        _participants.GetByIdAsync(participant.Id, Arg.Any<CancellationToken>()).Returns(participant);
        _rooms.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>())
               .Returns((VotingRound?)null);

        await _handler.HandleAsync(
            new DisconnectCommand(participant.Id, "conn"), CancellationToken.None);

        await _notifier.Received(1).NotifyUserLeft(
            Arg.Any<string>(), Arg.Any<UserLeftPayload>());
    }
}
