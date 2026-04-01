namespace Alignd.Application.Voting.StartRound;

public sealed record StartRoundCommand(string RoomCode, Guid ParticipantId, string? FreeTitle);
public sealed record RegisterConnectionCommand(string RoomCode, Guid ParticipantId, string ConnectionId);
public sealed record StartRoundResult(Guid RoundId);
