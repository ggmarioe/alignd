using Alignd.Application.Interfaces;
using Alignd.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alignd.Infrastructure.Persistence.Repositories;

public sealed class RoomRepository(AppDbContext db) : IRoomRepository
{
    public Task<Room?> GetByCodeAsync(string code, CancellationToken ct) =>
        db.Rooms
          .Include(r => r.Participants)
          .Include(r => r.Tasks.OrderBy(t => t.Order))
          .FirstOrDefaultAsync(r => r.Code == code.ToUpperInvariant() && r.IsActive, ct);

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Rooms
          .Include(r => r.Participants)
          .Include(r => r.Tasks.OrderBy(t => t.Order))
          .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, ct);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken ct) =>
        db.Rooms.AnyAsync(r => r.Code == code.ToUpperInvariant(), ct);

    public async Task AddAsync(Room room, CancellationToken ct) =>
        await db.Rooms.AddAsync(room, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
