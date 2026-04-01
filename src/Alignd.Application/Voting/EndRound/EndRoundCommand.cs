namespace Alignd.Application.Voting.EndRound;

public sealed record EndRoundCommand(string RoomCode, Guid ParticipantId);
