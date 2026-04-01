using Alignd.Application.Interfaces;
using Alignd.Domain.Enums;
using Alignd.SharedKernel;

namespace Alignd.Application.Participants.SetWatcher;

public sealed class SetWatcherHandler(
    IRoomRepository        rooms,
    IParticipantRepository participants,
    IRoomNotifier          notifier)
{
    public async Task<Result> HandleAsync(SetWatcherCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByCodeAsync(cmd.RoomCode, ct);
        if (room is null)
            return Result.NotFound("room.not_found", "Room not found.");

        var participant = await participants.GetByIdAsync(cmd.ParticipantId, ct);
        if (participant is null)
            return Result.NotFound("participant.not_found", "Participant not found.");

        if (participant.Role == ParticipantRole.Admin)
            return Result.Forbidden("role.admin_cannot_watch", "The admin cannot switch to watcher mode.");

        participant.Role = cmd.IsWatcher ? ParticipantRole.Watcher : ParticipantRole.Voter;
        await participants.SaveChangesAsync(ct);

        await notifier.NotifyUserJoined(cmd.RoomCode, new UserJoinedPayload(
            participant.Id, participant.Username,
            participant.Role.ToString().ToLowerInvariant()));

        return Result.Ok();
    }
}
