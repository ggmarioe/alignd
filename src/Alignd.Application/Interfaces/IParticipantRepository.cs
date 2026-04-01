using Alignd.Domain.Entities;

namespace Alignd.Application.Interfaces;

public interface IParticipantRepository
{
    Task<Participant?>       GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool>               ExistsInRoomAsync(Guid roomId, string username, CancellationToken ct);
    Task<List<Participant>>  GetConnectedByRoomAsync(Guid roomId, CancellationToken ct);
    Task                     SaveChangesAsync(CancellationToken ct);
}
