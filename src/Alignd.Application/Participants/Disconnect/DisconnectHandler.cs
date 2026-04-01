using Alignd.Application.Interfaces;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;

namespace Alignd.Application.Participants.Disconnect;

public sealed class DisconnectHandler(
    IRoomRepository        rooms,
    IParticipantRepository participants,
    IVotingRoundRepository rounds,
    IRoomNotifier          notifier)
{
    public async Task HandleAsync(DisconnectCommand cmd, CancellationToken ct)
    {
        var participant = await participants.GetByIdAsync(cmd.ParticipantId, ct);
        if (participant is null) return;

        var room = await rooms.GetByIdAsync(participant.RoomId, ct);
        if (room is null) return;

        participant.IsConnected  = false;
        participant.ConnectionId = null;

        var activeRound = await rounds.GetActiveByRoomAsync(room.Id, ct);
        if (activeRound is not null)
        {
            var hasVoted = activeRound.Votes.Any(v => v.ParticipantId == cmd.ParticipantId);
            if (!hasVoted && participant.Role != ParticipantRole.Watcher)
            {
                var abstain = Vote.Cast(activeRound.Id, participant.Id, "?");
                activeRound.AddVote(abstain);
            }
        }

        await participants.SaveChangesAsync(ct);

        await notifier.NotifyUserLeft(room.Code,
            new UserLeftPayload(participant.Id, participant.Username));

        if (room.AdminParticipantId == participant.Id)
            await notifier.NotifyAdminChanged(room.Code,
                new AdminChangedPayload(Guid.Empty, string.Empty));
    }
}
