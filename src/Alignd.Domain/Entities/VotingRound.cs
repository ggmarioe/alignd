using Alignd.Domain.Enums;

namespace Alignd.Domain.Entities;

public sealed class VotingRound
{
    public Guid        Id         { get; private set; } = Guid.NewGuid();
    public Guid        RoomId     { get; private set; }
    public Guid?       TaskItemId { get; private set; }
    public string?     FreeTitle  { get; private set; }
    public RoundStatus Status     { get; private set; } = RoundStatus.Pending;
    public DateTime?   StartedAt  { get; private set; }
    public DateTime?   EndedAt    { get; private set; }

    private readonly List<Vote> _votes = [];
    public IReadOnlyList<Vote> Votes => _votes.AsReadOnly();

    private VotingRound() { }

    public static VotingRound CreateForTask(Guid roomId, Guid taskItemId) => new()
    {
        RoomId     = roomId,
        TaskItemId = taskItemId,
        Status     = RoundStatus.Pending
    };

    public static VotingRound CreateFree(Guid roomId, string? freeTitle) => new()
    {
        RoomId    = roomId,
        FreeTitle = freeTitle,
        Status    = RoundStatus.Pending
    };

    public void Start()
    {
        Status    = RoundStatus.Active;
        StartedAt = DateTime.UtcNow;
    }

    public void End()
    {
        Status  = RoundStatus.Ended;
        EndedAt = DateTime.UtcNow;
    }

    public void AddVote(Vote vote) => _votes.Add(vote);

    public IReadOnlyList<string> TopVotes()
    {
        if (_votes.Count == 0) return [];

        var groups = _votes
            .GroupBy(v => v.Value)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var max = groups[0].Count;
        return groups.Where(g => g.Count == max)
                     .Select(g => g.Value)
                     .ToList();
    }
}
