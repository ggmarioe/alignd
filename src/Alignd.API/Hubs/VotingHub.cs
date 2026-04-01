using Alignd.Application.Participants.ClaimAdmin;
using Alignd.Application.Participants.Disconnect;
using Alignd.Application.Participants.SetWatcher;
using Alignd.Application.Voting.CastVote;
using Alignd.Application.Voting.EndRound;
using Alignd.Application.Voting.NextTask;
using Alignd.Application.Voting.StartRound;
using Alignd.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Alignd.API.Hubs;

[Authorize]
public sealed class VotingHub(
    StartRoundHandler  startRound,
    CastVoteHandler    castVote,
    EndRoundHandler    endRound,
    NextTaskHandler    nextTask,
    ClaimAdminHandler  claimAdmin,
    DisconnectHandler  disconnect,
    SetWatcherHandler  setWatcher) : VotingHubMarker
{
    public async Task JoinRoomAsync(string roomCode)
    {
        var participantId = GetParticipantId();
        if (participantId is null) { await SendError("auth.missing", "Participant identity not found."); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

        var result = await startRound.RegisterConnectionAsync(
            new RegisterConnectionCommand(roomCode, participantId.Value, Context.ConnectionId),
            Context.ConnectionAborted);

        if (result.IsFailure)
            await SendError(result.Errors[0].Code, result.Errors[0].Message);
    }

    public async Task StartRoundAsync(string roomCode, string? freeTitle = null)
    {
        var participantId = GetParticipantId();
        if (participantId is null) return;

        var result = await startRound.HandleAsync(
            new StartRoundCommand(roomCode, participantId.Value, freeTitle),
            Context.ConnectionAborted);

        if (result.IsFailure)
            await SendError(result.Errors[0].Code, result.Errors[0].Message);
    }

    public async Task CastVoteAsync(string roomCode, string value)
    {
        var participantId = GetParticipantId();
        if (participantId is null) return;

        var result = await castVote.HandleAsync(
            new CastVoteCommand(roomCode, participantId.Value, value),
            Context.ConnectionAborted);

        if (result.IsFailure)
            await SendError(result.Errors[0].Code, result.Errors[0].Message);
    }

    public async Task EndRoundAsync(string roomCode)
    {
        var participantId = GetParticipantId();
        if (participantId is null) return;

        var result = await endRound.HandleAsync(
            new EndRoundCommand(roomCode, participantId.Value),
            Context.ConnectionAborted);

        if (result.IsFailure)
            await SendError(result.Errors[0].Code, result.Errors[0].Message);
    }

    public async Task StartNextRoundAsync(string roomCode)
    {
        var participantId = GetParticipantId();
        if (participantId is null) return;

        var result = await nextTask.HandleAsync(
            new NextTaskCommand(roomCode, participantId.Value),
            Context.ConnectionAborted);

        if (result.IsFailure)
            await SendError(result.Errors[0].Code, result.Errors[0].Message);
    }

    public async Task ClaimAdminAsync(string roomCode)
    {
        var participantId = GetParticipantId();
        if (participantId is null) return;

        var result = await claimAdmin.HandleAsync(
            new ClaimAdminCommand(roomCode, participantId.Value),
            Context.ConnectionAborted);

        if (result.IsFailure)
            await SendError(result.Errors[0].Code, result.Errors[0].Message);
    }

    public async Task SetWatcherAsync(string roomCode, bool isWatcher)
    {
        var participantId = GetParticipantId();
        if (participantId is null) return;

        var result = await setWatcher.HandleAsync(
            new SetWatcherCommand(roomCode, participantId.Value, isWatcher),
            Context.ConnectionAborted);

        if (result.IsFailure)
            await SendError(result.Errors[0].Code, result.Errors[0].Message);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var participantId = GetParticipantId();
        if (participantId is not null)
            _ = disconnect.HandleAsync(
                new DisconnectCommand(participantId.Value, Context.ConnectionId),
                CancellationToken.None);

        await base.OnDisconnectedAsync(exception);
    }

    private Guid? GetParticipantId()
    {
        var claim = Context.User?.FindFirst("participantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private Task SendError(string code, string message) =>
        Clients.Caller.SendAsync("error", new { code, message });
}
