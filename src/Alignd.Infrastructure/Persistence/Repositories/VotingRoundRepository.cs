using Alignd.Application.Interfaces;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Alignd.Infrastructure.Persistence.Repositories;

public sealed class VotingRoundRepository(AppDbContext db) : IVotingRoundRepository
{
    public Task<VotingRound?> GetActiveByRoomAsync(Guid roomId, CancellationToken ct) =>
        db.VotingRounds
          .Include(vr => vr.Votes)
          .FirstOrDefaultAsync(
              vr => vr.RoomId == roomId && vr.Status == RoundStatus.Active, ct);

    public Task<VotingRound?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.VotingRounds
          .Include(vr => vr.Votes)
          .FirstOrDefaultAsync(vr => vr.Id == id, ct);

    public async Task AddAsync(VotingRound round, CancellationToken ct) =>
        await db.VotingRounds.AddAsync(round, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
