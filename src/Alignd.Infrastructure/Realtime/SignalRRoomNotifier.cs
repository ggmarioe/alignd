using System.Diagnostics.CodeAnalysis;
using Alignd.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Alignd.Infrastructure.Realtime;

[ExcludeFromCodeCoverage(Justification = "SignalR pass-through — requires a live hub; covered by integration tests.")]
public sealed class SignalRRoomNotifier(IHubContext<VotingHubMarker> hub) : IRoomNotifier
{
    public Task NotifyUserJoined(string roomCode, UserJoinedPayload payload) =>
        hub.Clients.Group(roomCode).SendAsync("user-joined", payload);

    public Task NotifyUserLeft(string roomCode, UserLeftPayload payload) =>
        hub.Clients.Group(roomCode).SendAsync("user-left", payload);

    public Task NotifyRoundStarted(string roomCode, RoundStartedPayload payload) =>
        hub.Clients.Group(roomCode).SendAsync("round-started", payload);

    public Task NotifyVoteCast(string roomCode, VoteCastPayload payload) =>
        hub.Clients.Group(roomCode).SendAsync("vote-cast", payload);

    public Task NotifyRoundEnded(string roomCode, RoundEndedPayload payload) =>
        hub.Clients.Group(roomCode).SendAsync("round-ended", payload);

    public Task NotifyTaskCompleted(string roomCode, TaskCompletedPayload payload) =>
        hub.Clients.Group(roomCode).SendAsync("task-completed", payload);

    public Task NotifyRoomReset(string roomCode, RoomResetPayload payload) =>
        hub.Clients.Group(roomCode).SendAsync("room-reset", payload);

    public Task NotifyAdminChanged(string roomCode, AdminChangedPayload payload) =>
        hub.Clients.Group(roomCode).SendAsync("admin-changed", payload);

    public Task NotifyRoomFinished(string roomCode) =>
        hub.Clients.Group(roomCode).SendAsync("room-finished", new { });
}
