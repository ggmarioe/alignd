namespace Alignd.Application.Interfaces;

public sealed record UserJoinedPayload(Guid ParticipantId, string Username, string Role);
public sealed record UserLeftPayload(Guid ParticipantId, string Username);
public sealed record RoundStartedPayload(Guid RoundId, string? TaskTitle, string? FreeTitle, string VotingType);
public sealed record VoteCastPayload(Guid ParticipantId);
public sealed record VoteResult(Guid ParticipantId, string Username, string Value);
public sealed record RoundEndedPayload(Guid RoundId, List<VoteResult> Votes, List<string> TopVotes);
public sealed record TaskCompletedPayload(Guid TaskId, Guid? NextTaskId);
public sealed record RoomResetPayload(Guid NewRoundId, string? SuggestedTitle);
public sealed record AdminChangedPayload(Guid NewAdminId, string Username);
