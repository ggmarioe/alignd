namespace Alignd.Application.Participants.JoinRoom;

public sealed record JoinRoomCommand(string RoomCode, string Username, bool AsWatcher);
public sealed record JoinRoomResult(Guid ParticipantId, string ParticipantToken);
