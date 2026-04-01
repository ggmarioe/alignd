namespace Alignd.Domain.Entities;

public sealed class Vote
{
    public Guid     Id            { get; private set; } = Guid.NewGuid();
    public Guid     RoundId       { get; private set; }
    public Guid     ParticipantId { get; private set; }
    public string   Value         { get; private set; } = default!;
    public DateTime CastAt        { get; private set; } = DateTime.UtcNow;

    private Vote() { }

    public static Vote Cast(Guid roundId, Guid participantId, string value) => new()
    {
        RoundId       = roundId,
        ParticipantId = participantId,
        Value         = value
    };
}
