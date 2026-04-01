using Alignd.Domain.Entities;

namespace Alignd.Application.Interfaces;

public interface IRoomRepository
{
    Task<Room?>  GetByCodeAsync(string code, CancellationToken ct);
    Task<Room?>  GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool>   ExistsByCodeAsync(string code, CancellationToken ct);
    Task         AddAsync(Room room, CancellationToken ct);
    Task         SaveChangesAsync(CancellationToken ct);
}
