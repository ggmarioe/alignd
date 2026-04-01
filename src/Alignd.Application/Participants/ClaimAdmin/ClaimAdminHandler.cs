using Alignd.Application.Interfaces;
using Alignd.SharedKernel;

namespace Alignd.Application.Participants.ClaimAdmin;

public sealed class ClaimAdminHandler(
    IRoomRepository        rooms,
    IParticipantRepository participants,
    IRoomNotifier          notifier)
{
    public async Task<Result> HandleAsync(ClaimAdminCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByCodeAsync(cmd.RoomCode, ct);
        if (room is null)
            return Result.NotFound("room.not_found", "Room not found.");

        var connectedParticipants = await participants.GetConnectedByRoomAsync(room.Id, ct);
        var hasActiveAdmin = connectedParticipants.Any(p => p.Id == room.AdminParticipantId);

        if (hasActiveAdmin)
            return Result.Conflict("admin.present", "The room already has an active admin.");

        var claimant = connectedParticipants.FirstOrDefault(p => p.Id == cmd.ParticipantId);
        if (claimant is null)
            return Result.NotFound("participant.not_found", "Participant not found.");

        var oldAdmin = room.Participants.FirstOrDefault(p => p.Id == room.AdminParticipantId);
        oldAdmin?.Demote();

        claimant.Promote();
        room.SetAdmin(claimant.Id);
        await rooms.SaveChangesAsync(ct);

        await notifier.NotifyAdminChanged(cmd.RoomCode,
            new AdminChangedPayload(claimant.Id, claimant.Username));

        return Result.Ok();
    }
}
