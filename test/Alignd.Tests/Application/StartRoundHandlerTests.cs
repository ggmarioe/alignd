using Alignd.Application.Interfaces;
using Alignd.Application.Voting.StartRound;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;
using Alignd.SharedKernel;
using NSubstitute;

namespace Alignd.Tests.Application;

/// <summary>
/// Tests for <see cref="StartRoundHandler"/>.
/// Uses NSubstitute for all dependency mocks.
/// </summary>
[TestFixture]
public sealed class StartRoundHandlerTests
{
    private IRoomRepository        _rooms        = null!;
    private IVotingRoundRepository _rounds       = null!;
    private IParticipantRepository _participants = null!;
    private IRoomNotifier          _notifier     = null!;
    private StartRoundHandler      _handler      = null!;

    [SetUp]
    public void SetUp()
    {
        _rooms        = Substitute.For<IRoomRepository>();
        _rounds       = Substitute.For<IVotingRoundRepository>();
        _participants = Substitute.For<IParticipantRepository>();
        _notifier     = Substitute.For<IRoomNotifier>();
        _handler      = new StartRoundHandler(_rooms, _rounds, _participants, _notifier);
    }

    private Room CreateRoomWithAdmin(out Guid adminId)
    {
        var room  = Room.Create(RoomCode.From("SWIFT-TIGER-42"), VoteType.Fibonacci);
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
            new StartRoundCommand("UNKNOWN-ROOM", Guid.NewGuid(), null), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task HandleAsync_WhenCallerIsNotTheAdmin_ReturnsForbidden()
    {
        var room = CreateRoomWithAdmin(out _);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);

        var result = await _handler.HandleAsync(
            new StartRoundCommand("SWIFT-TIGER-42", Guid.NewGuid(), null), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Forbidden),
            "Only the room admin may start a voting round");
    }

    [Test]
    public async Task HandleAsync_WhenARoundIsAlreadyActive_ReturnsConflict()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        var existingRound = VotingRound.CreateFree(room.Id, "Existing");
        existingRound.Start();
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>()).Returns(existingRound);

        var result = await _handler.HandleAsync(
            new StartRoundCommand("SWIFT-TIGER-42", adminId, null), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Conflict),
            "Starting a second round while one is already in progress must be rejected");
    }

    [Test]
    public async Task HandleAsync_WhenRoomHasUncompletedTasks_StartsTaskBasedRound()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        var task = TaskItem.Create(room.Id, "Story A", 1);
        room.AddTask(task);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>())
               .Returns((VotingRound?)null);

        var result = await _handler.HandleAsync(
            new StartRoundCommand("SWIFT-TIGER-42", adminId, null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,  Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Ok));
        });
    }

    [Test]
    public async Task HandleAsync_WhenRoomHasNoTasks_StartsFreeRound()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>())
               .Returns((VotingRound?)null);

        var result = await _handler.HandleAsync(
            new StartRoundCommand("SWIFT-TIGER-42", adminId, "Free topic"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True,
            "When there are no tasks, a free-form round should be started");
    }

    [Test]
    public async Task HandleAsync_OnSuccess_AddsAndSavesRound()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>())
               .Returns((VotingRound?)null);

        await _handler.HandleAsync(
            new StartRoundCommand("SWIFT-TIGER-42", adminId, null), CancellationToken.None);

        await _rounds.Received(1).AddAsync(Arg.Any<VotingRound>(), Arg.Any<CancellationToken>());
        await _rounds.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_OnSuccess_NotifiesRoomThatRoundStarted()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _rounds.GetActiveByRoomAsync(room.Id, Arg.Any<CancellationToken>())
               .Returns((VotingRound?)null);

        await _handler.HandleAsync(
            new StartRoundCommand("SWIFT-TIGER-42", adminId, null), CancellationToken.None);

        await _notifier.Received(1).NotifyRoundStarted(
            Arg.Any<string>(), Arg.Any<RoundStartedPayload>());
    }

    // ─── RegisterConnectionAsync ──────────────────────────────────────────────

    [Test]
    public async Task RegisterConnectionAsync_WhenParticipantDoesNotExist_ReturnsNotFound()
    {
        _participants.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                     .Returns((Participant?)null);

        var result = await _handler.RegisterConnectionAsync(
            new RegisterConnectionCommand("ROOM", Guid.NewGuid(), "conn-id"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task RegisterConnectionAsync_SetsConnectionId_AndMarksParticipantAsConnected()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Alice", ParticipantRole.Voter);
        _participants.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                     .Returns(participant);

        await _handler.RegisterConnectionAsync(
            new RegisterConnectionCommand("ROOM", participant.Id, "hub-conn-id"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(participant.ConnectionId, Is.EqualTo("hub-conn-id"));
            Assert.That(participant.IsConnected,  Is.True);
        });
    }

    [Test]
    public async Task RegisterConnectionAsync_OnSuccess_NotifiesRoomThatUserJoined()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Bob", ParticipantRole.Voter);
        _participants.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                     .Returns(participant);

        await _handler.RegisterConnectionAsync(
            new RegisterConnectionCommand("ROOM", participant.Id, "conn"), CancellationToken.None);

        await _notifier.Received(1).NotifyUserJoined(
            Arg.Any<string>(), Arg.Any<UserJoinedPayload>());
    }
}
