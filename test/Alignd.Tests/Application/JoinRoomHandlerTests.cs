using Alignd.Application.Interfaces;
using Alignd.Application.Participants.JoinRoom;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;
using Alignd.SharedKernel;
using NSubstitute;

namespace Alignd.Tests.Application;

/// <summary>
/// Tests for <see cref="JoinRoomHandler"/>.
/// Uses NSubstitute for all dependency mocks.
/// </summary>
[TestFixture]
public sealed class JoinRoomHandlerTests
{
    private IRoomRepository          _rooms        = null!;
    private IParticipantRepository   _participants = null!;
    private IProfanityFilter         _profanity    = null!;
    private IParticipantTokenService _tokenService = null!;
    private JoinRoomHandler          _handler      = null!;

    [SetUp]
    public void SetUp()
    {
        _rooms        = Substitute.For<IRoomRepository>();
        _participants = Substitute.For<IParticipantRepository>();
        _profanity    = Substitute.For<IProfanityFilter>();
        _tokenService = Substitute.For<IParticipantTokenService>();
        _handler      = new JoinRoomHandler(_rooms, _participants, _profanity, _tokenService);
    }

    private Room CreateRoomWithNoAdmin()
    {
        var room = Room.Create(RoomCode.From("SWIFT-TIGER-42"), VoteType.Fibonacci);
        return room;
    }

    private Room CreateRoomWithAdmin()
    {
        var room  = Room.Create(RoomCode.From("BRAVE-EAGLE-77"), VoteType.Fibonacci);
        var admin = Participant.Create(room.Id, "Admin", ParticipantRole.Admin);
        room.AddParticipant(admin);
        room.SetAdmin(admin.Id);
        return room;
    }

    [Test]
    public async Task HandleAsync_WhenUsernameTooShort_ReturnsUnprocessable()
    {
        var result = await _handler.HandleAsync(
            new JoinRoomCommand("ROOM-CODE", "A", false), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unprocessable),
            "A username shorter than 2 characters must be rejected");
    }

    [Test]
    public async Task HandleAsync_WhenUsernameIsEmpty_ReturnsUnprocessable()
    {
        var result = await _handler.HandleAsync(
            new JoinRoomCommand("ROOM-CODE", "", false), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unprocessable));
    }

    [Test]
    public async Task HandleAsync_WhenUsernameExceeds20Characters_ReturnsUnprocessable()
    {
        var longUsername = new string('x', 21);

        var result = await _handler.HandleAsync(
            new JoinRoomCommand("ROOM-CODE", longUsername, false), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unprocessable),
            "A username with more than 20 characters must be rejected");
    }

    [Test]
    public async Task HandleAsync_WhenUsernameContainsProfanity_ReturnsUnprocessable()
    {
        _profanity.IsProfane("badword").Returns(true);

        var result = await _handler.HandleAsync(
            new JoinRoomCommand("ROOM-CODE", "badword", false), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unprocessable),
            "A profane username must be rejected with an Unprocessable result");
    }

    [Test]
    public async Task HandleAsync_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        _profanity.IsProfane(Arg.Any<string>()).Returns(false);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns((Room?)null);

        var result = await _handler.HandleAsync(
            new JoinRoomCommand("MISSING-ROOM", "Alice", false), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task HandleAsync_WhenUsernameIsAlreadyTakenInRoom_ReturnsConflict()
    {
        var room = CreateRoomWithAdmin();
        _profanity.IsProfane(Arg.Any<string>()).Returns(false);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _participants.ExistsInRoomAsync(room.Id, "Alice", Arg.Any<CancellationToken>())
                     .Returns(true);

        var result = await _handler.HandleAsync(
            new JoinRoomCommand("BRAVE-EAGLE-77", "Alice", false), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Conflict),
            "Joining with an already-taken username must return a Conflict");
    }

    [Test]
    public async Task HandleAsync_WhenFirstParticipantJoins_IsAutomaticallyPromotedToAdmin()
    {
        var room = CreateRoomWithNoAdmin();
        _profanity.IsProfane(Arg.Any<string>()).Returns(false);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _participants.ExistsInRoomAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(false);
        _tokenService.Generate(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns("participant-token");

        await _handler.HandleAsync(
            new JoinRoomCommand("SWIFT-TIGER-42", "Alice", false), CancellationToken.None);

        Assert.That(room.AdminParticipantId, Is.Not.Null,
            "The first participant to join a room with no admin must become the admin");
    }

    [Test]
    public async Task HandleAsync_WhenRoomAlreadyHasAnAdmin_NewParticipantIsNotPromoted()
    {
        var room = CreateRoomWithAdmin();
        _profanity.IsProfane(Arg.Any<string>()).Returns(false);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _participants.ExistsInRoomAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(false);
        _tokenService.Generate(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns("token");

        var existingAdminId = room.AdminParticipantId;
        await _handler.HandleAsync(
            new JoinRoomCommand("BRAVE-EAGLE-77", "Bob", false), CancellationToken.None);

        Assert.That(room.AdminParticipantId, Is.EqualTo(existingAdminId),
            "Admin must not change when a second participant joins a room that already has one");
    }

    [Test]
    public async Task HandleAsync_WhenJoiningAsWatcher_ParticipantReceivesWatcherRole()
    {
        var room = CreateRoomWithAdmin();
        _profanity.IsProfane(Arg.Any<string>()).Returns(false);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _participants.ExistsInRoomAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(false);
        _tokenService.Generate(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns("token");

        await _handler.HandleAsync(
            new JoinRoomCommand("BRAVE-EAGLE-77", "Watcher1", true), CancellationToken.None);

        var newParticipant = room.Participants.Last();
        Assert.That(newParticipant.Role, Is.EqualTo(ParticipantRole.Watcher));
    }

    [Test]
    public async Task HandleAsync_OnSuccess_ReturnsCreatedResult_WithParticipantToken()
    {
        var room = CreateRoomWithAdmin();
        _profanity.IsProfane(Arg.Any<string>()).Returns(false);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _participants.ExistsInRoomAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(false);
        _tokenService.Generate(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns("the-participant-token");

        var result = await _handler.HandleAsync(
            new JoinRoomCommand("BRAVE-EAGLE-77", "Carol", false), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,                   Is.True);
            Assert.That(result.StatusCode,                  Is.EqualTo(ResultCode.Created));
            Assert.That(result.Value!.ParticipantToken,     Is.EqualTo("the-participant-token"));
            Assert.That(result.Value!.ParticipantId,        Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public async Task HandleAsync_OnSuccess_SavesRoomChanges()
    {
        var room = CreateRoomWithAdmin();
        _profanity.IsProfane(Arg.Any<string>()).Returns(false);
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _participants.ExistsInRoomAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(false);
        _tokenService.Generate(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns("token");

        await _handler.HandleAsync(
            new JoinRoomCommand("BRAVE-EAGLE-77", "Dave", false), CancellationToken.None);

        await _rooms.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
