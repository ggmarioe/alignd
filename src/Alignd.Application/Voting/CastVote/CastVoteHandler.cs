using System.Collections.Frozen;
using Alignd.Application.Interfaces;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.SharedKernel;

namespace Alignd.Application.Voting.CastVote;

public sealed class CastVoteHandler(
    IVotingRoundRepository rounds,
    IParticipantRepository participants,
    IRoomRepository        rooms,
    IRoomNotifier          notifier)
{
    private static readonly FrozenSet<string> FibValues =
        new[] { "1","2","3","5","8","13","21","?","☕" }.ToFrozenSet();

    private static readonly FrozenSet<string> ShirtValues =
        new[] { "XS","S","M","L","XL","XXL","?","☕" }.ToFrozenSet();

    public async Task<Result> HandleAsync(CastVoteCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByCodeAsync(cmd.RoomCode, ct);
        if (room is null)
            return Result.NotFound("room.not_found", "Room not found.");

        var participant = await participants.GetByIdAsync(cmd.ParticipantId, ct);
        if (participant is null)
            return Result.NotFound("participant.not_found", "Participant not found.");

        if (participant.Role == ParticipantRole.Watcher)
            return Result.Forbidden("vote.watcher", "Watchers cannot vote.");

        var round = await rounds.GetActiveByRoomAsync(room.Id, ct);
        if (round is null)
            return Result.Conflict("round.not_active", "No active round to vote on.");

        if (round.Votes.Any(v => v.ParticipantId == cmd.ParticipantId))
            return Result.Conflict("vote.already_cast", "You have already voted this round.");

        var allowed = room.VoteType == VoteType.Fibonacci ? FibValues : ShirtValues;
        if (!allowed.Contains(cmd.Value))
            return Result.Unprocessable("vote.invalid_value",
                $"'{cmd.Value}' is not a valid vote for this room.", "value");

        var vote = Vote.Cast(round.Id, cmd.ParticipantId, cmd.Value);
        round.AddVote(vote);
        await rounds.SaveChangesAsync(ct);

        await notifier.NotifyVoteCast(cmd.RoomCode, new VoteCastPayload(cmd.ParticipantId));

        var voters   = await participants.GetConnectedByRoomAsync(room.Id, ct);
        var voterIds = voters.Where(p => p.Role != ParticipantRole.Watcher).Select(p => p.Id).ToHashSet();
        var votedIds = round.Votes.Select(v => v.ParticipantId).ToHashSet();

        if (voterIds.SetEquals(votedIds))
            await RevealRound(round, voters, cmd.RoomCode, ct);

        return Result.Ok();
    }

    private async Task RevealRound(VotingRound round, List<Participant> allParticipants,
        string roomCode, CancellationToken ct)
    {
        round.End();
        await rounds.SaveChangesAsync(ct);

        var usernameMap = allParticipants.ToDictionary(p => p.Id, p => p.Username);
        var voteResults = round.Votes
            .Select(v => new VoteResult(v.ParticipantId,
                usernameMap.GetValueOrDefault(v.ParticipantId, "?"), v.Value))
            .ToList();

        await notifier.NotifyRoundEnded(roomCode, new RoundEndedPayload(
            round.Id, voteResults, round.TopVotes().ToList()));
    }
}
