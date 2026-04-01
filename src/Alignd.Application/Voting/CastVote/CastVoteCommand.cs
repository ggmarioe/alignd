namespace Alignd.Application.Voting.CastVote;

public sealed record CastVoteCommand(string RoomCode, Guid ParticipantId, string Value);
