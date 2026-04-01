namespace Alignd.Application.Interfaces;

public interface IRoomNotifier
{
    Task NotifyUserJoined(string roomCode, UserJoinedPayload payload);
    Task NotifyUserLeft(string roomCode, UserLeftPayload payload);
    Task NotifyRoundStarted(string roomCode, RoundStartedPayload payload);
    Task NotifyVoteCast(string roomCode, VoteCastPayload payload);
    Task NotifyRoundEnded(string roomCode, RoundEndedPayload payload);
    Task NotifyTaskCompleted(string roomCode, TaskCompletedPayload payload);
    Task NotifyRoomReset(string roomCode, RoomResetPayload payload);
    Task NotifyAdminChanged(string roomCode, AdminChangedPayload payload);
    Task NotifyRoomFinished(string roomCode);
}
