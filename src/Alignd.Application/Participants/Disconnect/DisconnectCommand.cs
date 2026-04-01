namespace Alignd.Application.Participants.Disconnect;

public sealed record DisconnectCommand(Guid ParticipantId, string ConnectionId);
