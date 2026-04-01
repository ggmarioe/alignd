using Alignd.Application.Interfaces;
using Alignd.Domain.Entities;
using Alignd.Domain.ValueObjects;
using Alignd.SharedKernel;

namespace Alignd.Application.Rooms.CreateRoom;

public sealed class CreateRoomHandler(
    IRoomRepository rooms,
    IAdminTokenService tokenService)
{
    public async Task<Result<CreateRoomResult>> HandleAsync(
        CreateRoomCommand cmd, CancellationToken ct)
    {
        RoomCode code;
        do { code = RoomCode.Generate(); }
        while (await rooms.ExistsByCodeAsync(code.Value, ct));

        var room = Room.Create(code, cmd.VoteType);
        await rooms.AddAsync(room, ct);
        await rooms.SaveChangesAsync(ct);

        var adminToken = tokenService.Generate(room.Id);
        return Result<CreateRoomResult>.Created(new CreateRoomResult(code.Value, adminToken));
    }

    public async Task<Result<RoomExistsResult>> RoomExistsAsync(
    string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result<RoomExistsResult>.Unprocessable(
                "room.code_required", "Room code is required.", "code");

        var exists = await rooms.ExistsByCodeAsync(code.ToUpperInvariant(), ct);

        if (!exists)
            return Result<RoomExistsResult>.NotFound(
                "room.not_found", $"Room '{code.ToUpperInvariant()}' does not exist or has expired.");

        return Result<RoomExistsResult>.Ok(new RoomExistsResult(true));
    }
}
