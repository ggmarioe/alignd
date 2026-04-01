namespace Alignd.Application.Tasks.UploadTasks;

public sealed record UploadTasksCommand(string RoomCode, Guid ParticipantId, string FileContent);
public sealed record UploadTasksResult(int TaskCount);
