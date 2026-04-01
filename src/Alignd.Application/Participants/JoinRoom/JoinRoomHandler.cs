using Alignd.Application.Interfaces;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.SharedKernel;

namespace Alignd.Application.Participants.JoinRoom;

public sealed class JoinRoomHandler(
    IRoomRepository            rooms,
    IParticipantRepository     participants,
    IProfanityFilter           profanity,
    IParticipantTokenService   tokenService)
{
    public async Task<Result<JoinRoomResult>> HandleAsync(
        JoinRoomCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Username) || cmd.Username.Length < 2)
            return Result<JoinRoomResult>.Unprocessable(
                "username.too_short", "Username must be at least 2 characters.", "username");

        if (cmd.Username.Length > 20)
            return Result<JoinRoomResult>.Unprocessable(
                "username.too_long", "Username cannot exceed 20 characters.", "username");

        if (profanity.IsProfane(cmd.Username))
            return Result<JoinRoomResult>.Unprocessable(
                "username.inappropriate", "That username is not allowed. Please choose another.", "username");

        var room = await rooms.GetByCodeAsync(cmd.RoomCode, ct);
        if (room is null)
            return Result<JoinRoomResult>.NotFound(
                "room.not_found", $"Room '{cmd.RoomCode}' does not exist or has expired.");

        var taken = await participants.ExistsInRoomAsync(room.Id, cmd.Username, ct);
        if (taken)
            return Result<JoinRoomResult>.Conflict(
                "username.taken", "Username is already taken in this room. Please choose another.");

        var role        = cmd.AsWatcher ? ParticipantRole.Watcher : ParticipantRole.Voter;
        var participant = Participant.Create(room.Id, cmd.Username, role);

        if (!room.Participants.Any(p => p.Role == ParticipantRole.Admin))
        {
            participant.Promote();
            room.SetAdmin(participant.Id);
        }

        room.AddParticipant(participant);
        await rooms.SaveChangesAsync(ct);

        var token = tokenService.Generate(participant.Id, room.Id);
        return Result<JoinRoomResult>.Created(new JoinRoomResult(participant.Id, token));
    }
}
