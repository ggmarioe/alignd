using Alignd.API.Extensions;
using Alignd.Application.Participants.JoinRoom;
using Alignd.Application.Rooms.CreateRoom;
using Alignd.Application.Tasks.UploadTasks;
using Alignd.Domain.Enums;
using Alignd.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace Alignd.API.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController(
    CreateRoomHandler createRoom,
    JoinRoomHandler joinRoom,
    UploadTasksHandler uploadTasks) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoomRequest req, CancellationToken ct) =>
        (await createRoom.HandleAsync(new CreateRoomCommand(req.VoteType), ct))
        .ToActionResult();

    [HttpGet("{code}/exists")]
    public async Task<IActionResult> Exists(string code, CancellationToken ct) =>
    (await createRoom.RoomExistsAsync(code, ct))
    .ToActionResult();

    [HttpPost("{code}/join")]
    public async Task<IActionResult> Join(
        string code,
        [FromBody] JoinRoomRequest req, CancellationToken ct) =>
        (await joinRoom.HandleAsync(
            new JoinRoomCommand(code, req.Username, req.AsWatcher), ct))
        .ToActionResult();

    [HttpPost("{code}/tasks")]
    public async Task<IActionResult> UploadTasks(
        string code,
        [FromBody] UploadTasksRequest req, CancellationToken ct)
    {
        var participantId = GetParticipantId();

        if (participantId is null)
            return Result.Unauthorized(
                "auth.missing", "Not authenticated.").ToActionResult();

        return (await uploadTasks.HandleAsync(
            new UploadTasksCommand(code, participantId.Value, req.FileContent), ct))
        .ToActionResult();
    }

    private Guid? GetParticipantId()
    {
        var claim = User.FindFirst("participantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record CreateRoomRequest(VoteType VoteType);
public sealed record JoinRoomRequest(string Username, bool AsWatcher = false);
public sealed record UploadTasksRequest(string FileContent);
