using Alignd.Application.Interfaces;
using Alignd.Domain.Entities;
using Alignd.SharedKernel;

namespace Alignd.Application.Tasks.UploadTasks;

public sealed class UploadTasksHandler(
    IRoomRepository rooms,
    ITaskRepository tasks)
{
    public async Task<Result<UploadTasksResult>> HandleAsync(
        UploadTasksCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByCodeAsync(cmd.RoomCode, ct);
        if (room is null)
            return Result<UploadTasksResult>.NotFound("room.not_found", "Room not found.");

        if (room.AdminParticipantId != cmd.ParticipantId)
            return Result<UploadTasksResult>.Forbidden(
                "tasks.not_admin", "Only the room admin can upload tasks.");

        if (room.Tasks.Any())
            return Result<UploadTasksResult>.Conflict(
                "tasks.already_uploaded", "Tasks have already been uploaded for this room.");

        var lines = ParseFileContent(cmd.FileContent);
        if (lines.Count == 0)
            return Result<UploadTasksResult>.Unprocessable(
                "tasks.empty", "The uploaded file contains no valid tasks.", "file");

        if (lines.Count > 200)
            return Result<UploadTasksResult>.Unprocessable(
                "tasks.too_many", "Cannot upload more than 200 tasks at once.", "file");

        var taskItems = lines
            .Select((title, index) => TaskItem.Create(room.Id, title, index + 1))
            .ToList();

        await tasks.AddRangeAsync(taskItems, ct);
        await tasks.SaveChangesAsync(ct);

        return Result<UploadTasksResult>.Created(new UploadTasksResult(taskItems.Count));
    }

    /// <summary>
    /// Parses both .txt (one line = one task) and .csv files.
    /// For CSV with multiple columns, all columns are joined with a space.
    /// </summary>
    private static List<string> ParseFileContent(string content)
    {
        var lines = content
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var result = new List<string>();

        foreach (var line in lines)
        {
            // Detect CSV by presence of comma
            if (line.Contains(','))
            {
                var parts = line.Split(',')
                    .Select(p => p.Trim().Trim('"'))
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();

                if (parts.Count > 0)
                    result.Add(string.Join(" ", parts));
            }
            else
            {
                result.Add(line);
            }
        }

        return result;
    }
}
