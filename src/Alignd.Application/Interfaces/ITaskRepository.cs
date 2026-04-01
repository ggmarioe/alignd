using Alignd.Domain.Entities;

namespace Alignd.Application.Interfaces;

public interface ITaskRepository
{
    Task<List<TaskItem>>  GetByRoomAsync(Guid roomId, CancellationToken ct);
    Task<TaskItem?>       GetNextPendingAsync(Guid roomId, CancellationToken ct);
    Task                  AddRangeAsync(IEnumerable<TaskItem> tasks, CancellationToken ct);
    Task                  SaveChangesAsync(CancellationToken ct);
}
