namespace Alignd.Application.Participants.SetWatcher;

public sealed record SetWatcherCommand(string RoomCode, Guid ParticipantId, bool IsWatcher);
