using System.Diagnostics.CodeAnalysis;
using Alignd.Application.Interfaces;
using Alignd.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alignd.Infrastructure.Persistence.Repositories;

[ExcludeFromCodeCoverage(Justification = "EF Core repository — requires a real database; covered by integration tests.")]
public sealed class TaskRepository(AppDbContext db) : ITaskRepository
{
    public Task<List<TaskItem>> GetByRoomAsync(Guid roomId, CancellationToken ct) =>
        db.TaskItems
          .Where(t => t.RoomId == roomId)
          .OrderBy(t => t.Order)
          .ToListAsync(ct);

    public Task<TaskItem?> GetNextPendingAsync(Guid roomId, CancellationToken ct) =>
        db.TaskItems
          .Where(t => t.RoomId == roomId && !t.IsCompleted)
          .OrderBy(t => t.Order)
          .FirstOrDefaultAsync(ct);

    public async Task AddRangeAsync(IEnumerable<TaskItem> tasks, CancellationToken ct) =>
        await db.TaskItems.AddRangeAsync(tasks, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
