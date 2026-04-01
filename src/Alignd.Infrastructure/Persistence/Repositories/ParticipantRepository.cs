using System.Diagnostics.CodeAnalysis;
using Alignd.Application.Interfaces;
using Alignd.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alignd.Infrastructure.Persistence.Repositories;

[ExcludeFromCodeCoverage(Justification = "EF Core repository — requires a real database; covered by integration tests.")]
public sealed class ParticipantRepository(AppDbContext db) : IParticipantRepository
{
    public Task<Participant?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Participants.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsInRoomAsync(Guid roomId, string username, CancellationToken ct) =>
        db.Participants.AnyAsync(
            p => p.RoomId == roomId && p.Username.ToLower() == username.ToLower(), ct);

    public Task<List<Participant>> GetConnectedByRoomAsync(Guid roomId, CancellationToken ct) =>
        db.Participants
          .Where(p => p.RoomId == roomId && p.IsConnected)
          .OrderBy(p => p.JoinedAt)
          .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
