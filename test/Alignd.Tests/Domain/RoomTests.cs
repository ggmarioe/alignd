using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;

namespace Alignd.Tests.Domain;

[TestFixture]
public sealed class RoomTests
{
    [Test]
    public void Create_SetsCode_FromProvidedRoomCode()
    {
        var code = RoomCode.From("SWIFT-TIGER-42");

        var room = Room.Create(code, VoteType.Fibonacci);

        Assert.That(room.Code, Is.EqualTo("SWIFT-TIGER-42"));
    }

    [Test]
    public void Create_SetsVoteType_FromProvidedVoteType()
    {
        var room = Room.Create(RoomCode.From("EPIC-HAWK-11"), VoteType.ShirtSize);

        Assert.That(room.VoteType, Is.EqualTo(VoteType.ShirtSize));
    }

    [Test]
    public void Create_NewRoom_IsActiveByDefault()
    {
        var room = Room.Create(RoomCode.From("GOLD-BEAR-20"), VoteType.Fibonacci);

        Assert.That(room.IsActive, Is.True,
            "A newly created room must be active");
    }

    [Test]
    public void Create_NewRoom_HasNoParticipants()
    {
        var room = Room.Create(RoomCode.From("IRON-LYNX-33"), VoteType.Fibonacci);

        Assert.That(room.Participants, Is.Empty,
            "A newly created room must not have any participants");
    }

    [Test]
    public void Create_NewRoom_HasNoAdminSet()
    {
        var room = Room.Create(RoomCode.From("JADE-VIPER-55"), VoteType.Fibonacci);

        Assert.That(room.AdminParticipantId, Is.Null,
            "A newly created room must not have an admin until one is assigned");
    }

    [Test]
    public void Create_AssignsNewGuid_AsId()
    {
        var room = Room.Create(RoomCode.From("KEEN-COBRA-66"), VoteType.Fibonacci);

        Assert.That(room.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void SetAdmin_AssignsTheGivenParticipantId_AsAdmin()
    {
        var room = Room.Create(RoomCode.From("FAST-RAVEN-77"), VoteType.Fibonacci);
        var adminId = Guid.NewGuid();

        room.SetAdmin(adminId);

        Assert.That(room.AdminParticipantId, Is.EqualTo(adminId));
    }

    [Test]
    public void SetAdmin_CalledTwice_OverwritesPreviousAdmin()
    {
        var room = Room.Create(RoomCode.From("DARK-SHARK-88"), VoteType.Fibonacci);
        var firstAdmin = Guid.NewGuid();
        var secondAdmin = Guid.NewGuid();

        room.SetAdmin(firstAdmin);
        room.SetAdmin(secondAdmin);

        Assert.That(room.AdminParticipantId, Is.EqualTo(secondAdmin));
    }

    [Test]
    public void AddParticipant_IncreasesParticipantCount_ByOne()
    {
        var room = Room.Create(RoomCode.From("BRAVE-WOLF-12"), VoteType.Fibonacci);
        var participant = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);

        room.AddParticipant(participant);

        Assert.That(room.Participants, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddParticipant_MultipleParticipants_AreAllPresent()
    {
        var room = Room.Create(RoomCode.From("CALM-EAGLE-99"), VoteType.Fibonacci);
        var alice = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        var bob   = Participant.Create(room.Id, "Bob",   ParticipantRole.Voter);

        room.AddParticipant(alice);
        room.AddParticipant(bob);

        Assert.That(room.Participants, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddTask_IncreasesTaskCount_ByOne()
    {
        var room = Room.Create(RoomCode.From("SWIFT-BEAR-10"), VoteType.Fibonacci);
        var task = TaskItem.Create(room.Id, "Story A", 1);

        room.AddTask(task);

        Assert.That(room.Tasks, Has.Count.EqualTo(1));
    }

    [Test]
    public void Rounds_NewRoom_IsEmpty()
    {
        var room = Room.Create(RoomCode.From("CALM-SHARK-15"), VoteType.Fibonacci);

        Assert.That(room.Rounds, Is.Empty,
            "A newly created room must have no voting rounds");
    }
}
