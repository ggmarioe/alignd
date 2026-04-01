using Alignd.Application.Interfaces;
using Alignd.SharedKernel;

namespace Alignd.Application.Voting.EndRound;

public sealed class EndRoundHandler(
    IRoomRepository        rooms,
    IVotingRoundRepository rounds,
    IParticipantRepository participants,
    IRoomNotifier          notifier)
{
    public async Task<Result> HandleAsync(EndRoundCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByCodeAsync(cmd.RoomCode, ct);
        if (room is null)
            return Result.NotFound("room.not_found", "Room not found.");

        if (room.AdminParticipantId != cmd.ParticipantId)
            return Result.Forbidden("round.not_admin", "Only the room admin can end a round.");

        var round = await rounds.GetActiveByRoomAsync(room.Id, ct);
        if (round is null)
            return Result.Conflict("round.not_active", "No active round to end.");

        round.End();
        await rounds.SaveChangesAsync(ct);

        var allParticipants = await participants.GetConnectedByRoomAsync(room.Id, ct);
        var usernameMap     = allParticipants.ToDictionary(p => p.Id, p => p.Username);
        var voteResults     = round.Votes
            .Select(v => new VoteResult(v.ParticipantId,
                usernameMap.GetValueOrDefault(v.ParticipantId, "?"), v.Value))
            .ToList();

        await notifier.NotifyRoundEnded(cmd.RoomCode, new RoundEndedPayload(
            round.Id, voteResults, round.TopVotes().ToList()));

        return Result.Ok();
    }
}
