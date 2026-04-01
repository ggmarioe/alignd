using Alignd.Application.Interfaces;
using Alignd.SharedKernel;

namespace Alignd.Application.Voting.NextTask;

public sealed class NextTaskHandler(
    IRoomRepository rooms,
    ITaskRepository tasks,
    IRoomNotifier   notifier)
{
    public async Task<Result> HandleAsync(NextTaskCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByCodeAsync(cmd.RoomCode, ct);
        if (room is null)
            return Result.NotFound("room.not_found", "Room not found.");

        if (room.AdminParticipantId != cmd.ParticipantId)
            return Result.Forbidden("round.not_admin", "Only the room admin can advance to the next task.");

        var completedTask = room.Tasks.Where(t => !t.IsCompleted).OrderBy(t => t.Order).FirstOrDefault();

        if (completedTask is not null)
        {
            completedTask.Complete();
            await tasks.SaveChangesAsync(ct);

            var nextTask = room.Tasks.Where(t => !t.IsCompleted).OrderBy(t => t.Order).FirstOrDefault();

            if (nextTask is null)
            {
                await notifier.NotifyRoomFinished(cmd.RoomCode);
                return Result.Ok();
            }

            await notifier.NotifyTaskCompleted(cmd.RoomCode,
                new TaskCompletedPayload(completedTask.Id, nextTask.Id));

            return Result.Ok();
        }

        await notifier.NotifyRoomReset(cmd.RoomCode, new RoomResetPayload(Guid.NewGuid(), null));
        return Result.Ok();
    }
}
