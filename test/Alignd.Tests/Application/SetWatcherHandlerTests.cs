using Alignd.Application.Interfaces;
using Alignd.Application.Participants.SetWatcher;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;
using Alignd.SharedKernel;
using Moq;

namespace Alignd.Tests.Application;

/// <summary>
/// Tests for <see cref="SetWatcherHandler"/>.
/// Uses Moq for all dependency mocks.
/// </summary>
[TestFixture]
public sealed class SetWatcherHandlerTests
{
    private Mock<IRoomRepository>        _roomsMock        = null!;
    private Mock<IParticipantRepository> _participantsMock = null!;
    private Mock<IRoomNotifier>          _notifierMock     = null!;
    private SetWatcherHandler            _handler          = null!;

    [SetUp]
    public void SetUp()
    {
        _roomsMock        = new Mock<IRoomRepository>();
        _participantsMock = new Mock<IParticipantRepository>();
        _notifierMock     = new Mock<IRoomNotifier>();
        _handler          = new SetWatcherHandler(
            _roomsMock.Object, _participantsMock.Object, _notifierMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Room?)null);

        var result = await _handler.HandleAsync(
            new SetWatcherCommand("MISSING", Guid.NewGuid(), true), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task HandleAsync_WhenParticipantDoesNotExist_ReturnsNotFound()
    {
        var room = Room.Create(RoomCode.From("JADE-VIPER-55"), VoteType.Fibonacci);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Participant?)null);

        var result = await _handler.HandleAsync(
            new SetWatcherCommand("JADE-VIPER-55", Guid.NewGuid(), true), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task HandleAsync_WhenParticipantIsAdmin_ReturnsForbidden()
    {
        var room  = Room.Create(RoomCode.From("JADE-VIPER-55"), VoteType.Fibonacci);
        var admin = Participant.Create(room.Id, "Admin", ParticipantRole.Admin);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(admin);

        var result = await _handler.HandleAsync(
            new SetWatcherCommand("JADE-VIPER-55", admin.Id, true), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Forbidden),
            "The room admin cannot switch to watcher mode");
    }

    [Test]
    public async Task HandleAsync_WhenIsWatcherTrue_SetsParticipantRoleToWatcher()
    {
        var room        = Room.Create(RoomCode.From("JADE-VIPER-55"), VoteType.Fibonacci);
        var participant = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(participant.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(participant);

        await _handler.HandleAsync(
            new SetWatcherCommand("JADE-VIPER-55", participant.Id, true),
            CancellationToken.None);

        Assert.That(participant.Role, Is.EqualTo(ParticipantRole.Watcher),
            "Setting IsWatcher=true must assign the Watcher role");
    }

    [Test]
    public async Task HandleAsync_WhenIsWatcherFalse_SetsParticipantRoleToVoter()
    {
        var room        = Room.Create(RoomCode.From("JADE-VIPER-55"), VoteType.Fibonacci);
        var participant = Participant.Create(room.Id, "Bob", ParticipantRole.Watcher);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(participant.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(participant);

        await _handler.HandleAsync(
            new SetWatcherCommand("JADE-VIPER-55", participant.Id, false),
            CancellationToken.None);

        Assert.That(participant.Role, Is.EqualTo(ParticipantRole.Voter),
            "Setting IsWatcher=false must revert the participant back to Voter");
    }

    [Test]
    public async Task HandleAsync_OnSuccess_SavesParticipantChanges()
    {
        var room        = Room.Create(RoomCode.From("JADE-VIPER-55"), VoteType.Fibonacci);
        var participant = Participant.Create(room.Id, "Carol", ParticipantRole.Voter);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(participant.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(participant);

        await _handler.HandleAsync(
            new SetWatcherCommand("JADE-VIPER-55", participant.Id, true), CancellationToken.None);

        _participantsMock.Verify(
            p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_OnSuccess_NotifiesRoomOfRoleChange()
    {
        var room        = Room.Create(RoomCode.From("JADE-VIPER-55"), VoteType.Fibonacci);
        var participant = Participant.Create(room.Id, "Dave", ParticipantRole.Voter);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(participant.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(participant);

        await _handler.HandleAsync(
            new SetWatcherCommand("JADE-VIPER-55", participant.Id, true), CancellationToken.None);

        _notifierMock.Verify(
            n => n.NotifyUserJoined(It.IsAny<string>(), It.IsAny<UserJoinedPayload>()),
            Times.Once, "A role change notification must be broadcast to the room");
    }

    [Test]
    public async Task HandleAsync_OnSuccess_ReturnsOkResult()
    {
        var room        = Room.Create(RoomCode.From("JADE-VIPER-55"), VoteType.Fibonacci);
        var participant = Participant.Create(room.Id, "Eve", ParticipantRole.Voter);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(participant.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(participant);

        var result = await _handler.HandleAsync(
            new SetWatcherCommand("JADE-VIPER-55", participant.Id, true), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Ok));
    }
}
