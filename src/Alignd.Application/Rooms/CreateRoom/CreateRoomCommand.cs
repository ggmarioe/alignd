using Alignd.Domain.Enums;

namespace Alignd.Application.Rooms.CreateRoom;

public sealed record CreateRoomCommand(VoteType VoteType);
public sealed record CreateRoomResult(string RoomCode, string AdminToken);
public sealed record RoomExistsResult(bool Exists);
