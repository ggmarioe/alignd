using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;

namespace Alignd.Domain.Entities;

public sealed class Room
{
    public Guid     Id                  { get; private set; } = Guid.NewGuid();
    public string   Code                { get; private set; } = default!;
    public VoteType VoteType            { get; private set; }
    public Guid?    AdminParticipantId  { get; private set; }
    public bool     IsActive            { get; private set; } = true;
    public DateTime CreatedAt           { get; private set; } = DateTime.UtcNow;

    private readonly List<Participant>  _participants = [];
    private readonly List<VotingRound>  _rounds       = [];
    private readonly List<TaskItem>     _tasks        = [];

    public IReadOnlyList<Participant>  Participants => _participants.AsReadOnly();
    public IReadOnlyList<VotingRound>  Rounds       => _rounds.AsReadOnly();
    public IReadOnlyList<TaskItem>     Tasks        => _tasks.AsReadOnly();

    private Room() { }

    public static Room Create(RoomCode code, VoteType voteType) => new()
    {
        Code     = code.Value,
        VoteType = voteType
    };

    public void SetAdmin(Guid participantId) => AdminParticipantId = participantId;
    public void AddParticipant(Participant p) => _participants.Add(p);
    public void AddTask(TaskItem t)           => _tasks.Add(t);
}
