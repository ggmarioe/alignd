using Alignd.Application.Interfaces;
using Alignd.Application.Participants.ClaimAdmin;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;
using Alignd.SharedKernel;
using NSubstitute;

namespace Alignd.Tests.Application;

/// <summary>
/// Tests for <see cref="ClaimAdminHandler"/>.
/// Uses NSubstitute for all dependency mocks.
/// </summary>
[TestFixture]
public sealed class ClaimAdminHandlerTests
{
    private IRoomRepository        _rooms        = null!;
    private IParticipantRepository _participants = null!;
    private IRoomNotifier          _notifier     = null!;
    private ClaimAdminHandler      _handler      = null!;

    [SetUp]
    public void SetUp()
    {
        _rooms        = Substitute.For<IRoomRepository>();
        _participants = Substitute.For<IParticipantRepository>();
        _notifier     = Substitute.For<IRoomNotifier>();
        _handler      = new ClaimAdminHandler(_rooms, _participants, _notifier);
    }

    [Test]
    public async Task HandleAsync_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns((Room?)null);

        var result = await _handler.HandleAsync(
            new ClaimAdminCommand("MISSING", Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task HandleAsync_WhenCurrentAdminIsStillConnected_ReturnsConflict()
    {
        var room  = Room.Create(RoomCode.From("IRON-LYNX-33"), VoteType.Fibonacci);
        var admin = Participant.Create(room.Id, "Admin", ParticipantRole.Admin);
        room.AddParticipant(admin);
        room.SetAdmin(admin.Id);

        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        // Admin is in the connected list, so the room still has an active admin
        _participants.GetConnectedByRoomAsync(room.Id, Arg.Any<CancellationToken>())
                     .Returns(new List<Participant> { admin });

        var result = await _handler.HandleAsync(
            new ClaimAdminCommand("IRON-LYNX-33", Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Conflict),
            "Claiming admin must be blocked when the current admin is still connected");
    }

    [Test]
    public async Task HandleAsync_WhenCurrentAdminIsDisconnected_AndClaimantNotFound_ReturnsNotFound()
    {
        var room  = Room.Create(RoomCode.From("IRON-LYNX-33"), VoteType.Fibonacci);
        var admin = Participant.Create(room.Id, "Admin", ParticipantRole.Admin);
        room.SetAdmin(admin.Id);

        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        // No connected participants — admin is gone but the claimant is also absent
        _participants.GetConnectedByRoomAsync(room.Id, Arg.Any<CancellationToken>())
                     .Returns(new List<Participant>());

        var result = await _handler.HandleAsync(
            new ClaimAdminCommand("IRON-LYNX-33", Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound),
            "If the claimant is not found among connected participants, return NotFound");
    }

    [Test]
    public async Task HandleAsync_WhenAdminIsDisconnected_PromotesClaimant_AndDemotesOldAdmin()
    {
        var room     = Room.Create(RoomCode.From("IRON-LYNX-33"), VoteType.Fibonacci);
        var oldAdmin = Participant.Create(room.Id, "OldAdmin", ParticipantRole.Admin);
        var claimant = Participant.Create(room.Id, "Claimant", ParticipantRole.Voter);
        room.AddParticipant(oldAdmin);
        room.AddParticipant(claimant);
        room.SetAdmin(oldAdmin.Id);

        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        // Only claimant is connected — old admin disconnected
        _participants.GetConnectedByRoomAsync(room.Id, Arg.Any<CancellationToken>())
                     .Returns(new List<Participant> { claimant });

        await _handler.HandleAsync(
            new ClaimAdminCommand("IRON-LYNX-33", claimant.Id), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(claimant.Role, Is.EqualTo(ParticipantRole.Admin),
                "The claimant must be promoted to Admin");
            Assert.That(oldAdmin.Role, Is.EqualTo(ParticipantRole.Voter),
                "The old admin must be demoted to Voter");
            Assert.That(room.AdminParticipantId, Is.EqualTo(claimant.Id),
                "The room's admin reference must point to the new admin");
        });
    }

    [Test]
    public async Task HandleAsync_OnSuccess_NotifiesRoomOfAdminChange()
    {
        var room     = Room.Create(RoomCode.From("IRON-LYNX-33"), VoteType.Fibonacci);
        var claimant = Participant.Create(room.Id, "Claimant", ParticipantRole.Voter);
        room.AddParticipant(claimant);
        room.SetAdmin(Guid.NewGuid()); // disconnected admin

        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _participants.GetConnectedByRoomAsync(room.Id, Arg.Any<CancellationToken>())
                     .Returns(new List<Participant> { claimant });

        await _handler.HandleAsync(
            new ClaimAdminCommand("IRON-LYNX-33", claimant.Id), CancellationToken.None);

        await _notifier.Received(1).NotifyAdminChanged(
            Arg.Any<string>(), Arg.Any<AdminChangedPayload>());
    }

    [Test]
    public async Task HandleAsync_OnSuccess_ReturnsOkResult()
    {
        var room     = Room.Create(RoomCode.From("IRON-LYNX-33"), VoteType.Fibonacci);
        var claimant = Participant.Create(room.Id, "Claimant", ParticipantRole.Voter);
        room.AddParticipant(claimant);
        room.SetAdmin(Guid.NewGuid());

        _rooms.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(room);
        _participants.GetConnectedByRoomAsync(room.Id, Arg.Any<CancellationToken>())
                     .Returns(new List<Participant> { claimant });

        var result = await _handler.HandleAsync(
            new ClaimAdminCommand("IRON-LYNX-33", claimant.Id), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Ok));
    }
}
