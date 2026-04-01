using Alignd.Application.Interfaces;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.SharedKernel;

namespace Alignd.Application.Voting.StartRound;

public sealed class StartRoundHandler(
    IRoomRepository        rooms,
    IVotingRoundRepository rounds,
    IParticipantRepository participants,
    IRoomNotifier          notifier)
{
    public async Task<Result<StartRoundResult>> HandleAsync(
        StartRoundCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByCodeAsync(cmd.RoomCode, ct);
        if (room is null)
            return Result<StartRoundResult>.NotFound("room.not_found", $"Room '{cmd.RoomCode}' not found.");

        if (room.AdminParticipantId != cmd.ParticipantId)
            return Result<StartRoundResult>.Forbidden("round.not_admin", "Only the room admin can start a round.");

        var existing = await rounds.GetActiveByRoomAsync(room.Id, ct);
        if (existing is not null)
            return Result<StartRoundResult>.Conflict("round.already_active", "A round is already in progress.");

        VotingRound round;
        string?     taskTitle = null;

        var nextTask = room.Tasks.Where(t => !t.IsCompleted).OrderBy(t => t.Order).FirstOrDefault();
        if (nextTask is not null)
        {
            round     = VotingRound.CreateForTask(room.Id, nextTask.Id);
            taskTitle = nextTask.Title;
        }
        else
        {
            round = VotingRound.CreateFree(room.Id, cmd.FreeTitle);
        }

        round.Start();
        await rounds.AddAsync(round, ct);
        await rounds.SaveChangesAsync(ct);

        await notifier.NotifyRoundStarted(cmd.RoomCode, new RoundStartedPayload(
            round.Id, taskTitle, cmd.FreeTitle,
            room.VoteType.ToString().ToLowerInvariant()));

        return Result<StartRoundResult>.Ok(new StartRoundResult(round.Id));
    }

    public async Task<Result> RegisterConnectionAsync(
        RegisterConnectionCommand cmd, CancellationToken ct)
    {
        var participant = await participants.GetByIdAsync(cmd.ParticipantId, ct);
        if (participant is null)
            return Result.NotFound("participant.not_found", "Participant not found.");

        participant.ConnectionId = cmd.ConnectionId;
        participant.IsConnected  = true;
        await participants.SaveChangesAsync(ct);

        await notifier.NotifyUserJoined(cmd.RoomCode, new UserJoinedPayload(
            participant.Id,
            participant.Username,
            participant.Role.ToString().ToLowerInvariant()));

        return Result.Ok();
    }
}
