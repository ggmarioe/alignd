using Alignd.Application.Interfaces;
using Alignd.Application.Tasks.UploadTasks;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;
using Alignd.SharedKernel;
using Moq;

namespace Alignd.Tests.Application;

/// <summary>
/// Tests for <see cref="UploadTasksHandler"/>.
/// Uses Moq for all dependency mocks.
/// </summary>
[TestFixture]
public sealed class UploadTasksHandlerTests
{
    private Mock<IRoomRepository> _roomsMock = null!;
    private Mock<ITaskRepository> _tasksMock = null!;
    private UploadTasksHandler    _handler   = null!;

    [SetUp]
    public void SetUp()
    {
        _roomsMock = new Mock<IRoomRepository>();
        _tasksMock = new Mock<ITaskRepository>();
        _handler   = new UploadTasksHandler(_roomsMock.Object, _tasksMock.Object);
    }

    private Room CreateRoomWithAdmin(out Guid adminId)
    {
        var room  = Room.Create(RoomCode.From("FAST-RAVEN-77"), VoteType.Fibonacci);
        var admin = Participant.Create(room.Id, "Admin", ParticipantRole.Admin);
        room.AddParticipant(admin);
        room.SetAdmin(admin.Id);
        adminId = admin.Id;
        return room;
    }

    [Test]
    public async Task HandleAsync_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Room?)null);

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("MISSING", Guid.NewGuid(), "Story A"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task HandleAsync_WhenCallerIsNotTheAdmin_ReturnsForbidden()
    {
        var room = CreateRoomWithAdmin(out _);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", Guid.NewGuid(), "Story A"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Forbidden),
            "Only the room admin may upload tasks");
    }

    [Test]
    public async Task HandleAsync_WhenTasksAreAlreadyUploaded_ReturnsConflict()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        var existingTask = TaskItem.Create(room.Id, "Pre-existing task", 1);
        room.AddTask(existingTask);

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", adminId, "Story B"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Conflict),
            "Tasks may only be uploaded once per room");
    }

    [Test]
    public async Task HandleAsync_WhenFileIsEmpty_ReturnsUnprocessable()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", adminId, ""), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unprocessable),
            "An empty file must be rejected");
    }

    [Test]
    public async Task HandleAsync_WhenFileContainsOnlyBlankLines_ReturnsUnprocessable()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", adminId, "\n\n\n"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unprocessable));
    }

    [Test]
    public async Task HandleAsync_WhenFileHasMoreThan200Lines_ReturnsUnprocessable()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var content = string.Join("\n", Enumerable.Range(1, 201).Select(i => $"Story {i}"));

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", adminId, content), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unprocessable),
            "More than 200 tasks in a single upload must be rejected");
    }

    [Test]
    public async Task HandleAsync_WhenFileHasExactly200Lines_AcceptsUpload()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var content = string.Join("\n", Enumerable.Range(1, 200).Select(i => $"Story {i}"));

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", adminId, content), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True,
            "Exactly 200 tasks must be accepted");
    }

    [Test]
    public async Task HandleAsync_WithPlainTextFile_ParsesEachLineAsASeparateTask()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var content = "Story A\nStory B\nStory C";

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", adminId, content), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,          Is.True);
            Assert.That(result.Value!.TaskCount,   Is.EqualTo(3));
        });
    }

    [Test]
    public async Task HandleAsync_WithCsvFile_JoinsColumnsWithSpaceForEachRow()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var content = "Story A,Do the thing\nStory B,Another thing";

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", adminId, content), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,         Is.True);
            Assert.That(result.Value!.TaskCount,  Is.EqualTo(2));
        });
    }

    [Test]
    public async Task HandleAsync_WithWindowsLineEndings_ParsesAllLinesCorrectly()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var content = "Story A\r\nStory B\r\nStory C";

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", adminId, content), CancellationToken.None);

        Assert.That(result.Value!.TaskCount, Is.EqualTo(3),
            "Windows-style line endings (CRLF) must be handled correctly");
    }

    [Test]
    public async Task HandleAsync_OnSuccess_PersistsTasksAndSavesChanges()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", adminId, "Story A\nStory B"),
            CancellationToken.None);

        _tasksMock.Verify(
            t => t.AddRangeAsync(It.IsAny<List<TaskItem>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _tasksMock.Verify(
            t => t.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_OnSuccess_ReturnsCreatedResult_WithCorrectTaskCount()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var result = await _handler.HandleAsync(
            new UploadTasksCommand("FAST-RAVEN-77", adminId, "Story A\nStory B\nStory C"),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode,        Is.EqualTo(ResultCode.Created));
            Assert.That(result.Value!.TaskCount,  Is.EqualTo(3));
        });
    }
}
