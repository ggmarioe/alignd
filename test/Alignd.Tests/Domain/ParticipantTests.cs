using Alignd.Domain.Entities;
using Alignd.Domain.Enums;

namespace Alignd.Tests.Domain;

[TestFixture]
public sealed class ParticipantTests
{
    [Test]
    public void Create_SetsUsername_FromProvidedValue()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Alice", ParticipantRole.Voter);

        Assert.That(participant.Username, Is.EqualTo("Alice"));
    }

    [Test]
    public void Create_SetsRole_FromProvidedValue()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Bob", ParticipantRole.Watcher);

        Assert.That(participant.Role, Is.EqualTo(ParticipantRole.Watcher));
    }

    [Test]
    public void Create_SetsRoomId_FromProvidedValue()
    {
        var roomId = Guid.NewGuid();

        var participant = Participant.Create(roomId, "Carol", ParticipantRole.Voter);

        Assert.That(participant.RoomId, Is.EqualTo(roomId));
    }

    [Test]
    public void Create_NewParticipant_IsDisconnectedByDefault()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Dave", ParticipantRole.Voter);

        Assert.That(participant.IsConnected, Is.False,
            "A newly created participant should not be connected until they join a SignalR hub");
    }

    [Test]
    public void Create_NewParticipant_HasNoConnectionId()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Eve", ParticipantRole.Voter);

        Assert.That(participant.ConnectionId, Is.Null);
    }

    [Test]
    public void Create_AssignsNewGuid_AsId()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Frank", ParticipantRole.Voter);

        Assert.That(participant.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Promote_ChangesRole_ToAdmin()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Grace", ParticipantRole.Voter);

        participant.Promote();

        Assert.That(participant.Role, Is.EqualTo(ParticipantRole.Admin),
            "Promote() must elevate any participant to the Admin role");
    }

    [Test]
    public void Demote_ChangesRole_ToVoter()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Hank", ParticipantRole.Admin);

        participant.Demote();

        Assert.That(participant.Role, Is.EqualTo(ParticipantRole.Voter),
            "Demote() must lower any participant to the Voter role");
    }

    [Test]
    public void Promote_ThenDemote_LeavesParticipant_AsVoter()
    {
        var participant = Participant.Create(Guid.NewGuid(), "Iris", ParticipantRole.Voter);

        participant.Promote();
        participant.Demote();

        Assert.That(participant.Role, Is.EqualTo(ParticipantRole.Voter));
    }

    [TestCase(ParticipantRole.Voter)]
    [TestCase(ParticipantRole.Watcher)]
    [TestCase(ParticipantRole.Admin)]
    public void Create_AcceptsAllValidRoles(ParticipantRole role)
    {
        var participant = Participant.Create(Guid.NewGuid(), "Jack", role);

        Assert.That(participant.Role, Is.EqualTo(role));
    }
}
