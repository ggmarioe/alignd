using Alignd.Domain.Enums;

namespace Alignd.Domain.Entities;

public sealed class Participant
{
    public Guid             Id           { get; private set; } = Guid.NewGuid();
    public Guid             RoomId       { get; private set; }
    public string           Username     { get; private set; } = default!;
    public ParticipantRole  Role         { get; set; }
    public string?          ConnectionId { get; set; }
    public bool             IsConnected  { get; set; }
    public DateTime         JoinedAt     { get; private set; } = DateTime.UtcNow;

    private Participant() { }

    public static Participant Create(Guid roomId, string username, ParticipantRole role) => new()
    {
        RoomId   = roomId,
        Username = username,
        Role     = role
    };

    public void Promote() => Role = ParticipantRole.Admin;
    public void Demote()  => Role = ParticipantRole.Voter;
}
