using Alignd.Domain.Entities;

namespace Alignd.Application.Interfaces;

public interface IVotingRoundRepository
{
    Task<VotingRound?>  GetActiveByRoomAsync(Guid roomId, CancellationToken ct);
    Task<VotingRound?>  GetByIdAsync(Guid id, CancellationToken ct);
    Task                AddAsync(VotingRound round, CancellationToken ct);
    Task                SaveChangesAsync(CancellationToken ct);
}
