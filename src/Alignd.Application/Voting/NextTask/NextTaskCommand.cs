namespace Alignd.Application.Voting.NextTask;

public sealed record NextTaskCommand(string RoomCode, Guid ParticipantId);
