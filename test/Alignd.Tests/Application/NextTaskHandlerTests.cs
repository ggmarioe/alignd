using Alignd.Application.Interfaces;
using Alignd.Application.Voting.NextTask;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;
using Alignd.SharedKernel;
using Moq;

namespace Alignd.Tests.Application;

/// <summary>
/// Tests for <see cref="NextTaskHandler"/>.
/// Uses Moq for all dependency mocks.
/// </summary>
[TestFixture]
public sealed class NextTaskHandlerTests
{
    private Mock<IRoomRepository>  _roomsMock    = null!;
    private Mock<ITaskRepository>  _tasksMock    = null!;
    private Mock<IRoomNotifier>    _notifierMock = null!;
    private NextTaskHandler        _handler      = null!;

    [SetUp]
    public void SetUp()
    {
        _roomsMock    = new Mock<IRoomRepository>();
        _tasksMock    = new Mock<ITaskRepository>();
        _notifierMock = new Mock<IRoomNotifier>();
        _handler      = new NextTaskHandler(_roomsMock.Object, _tasksMock.Object, _notifierMock.Object);
    }

    private Room CreateRoomWithAdmin(out Guid adminId)
    {
        var room  = Room.Create(RoomCode.From("KEEN-COBRA-66"), VoteType.Fibonacci);
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
            new NextTaskCommand("MISSING", Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task HandleAsync_WhenCallerIsNotTheAdmin_ReturnsForbidden()
    {
        var room = CreateRoomWithAdmin(out _);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var result = await _handler.HandleAsync(
            new NextTaskCommand("KEEN-COBRA-66", Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Forbidden),
            "Only the admin may advance to the next task");
    }

    [Test]
    public async Task HandleAsync_WhenThereIsAnUncompletedTask_MarksItAsCompleted()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        var task = TaskItem.Create(room.Id, "Story A", 1);
        room.AddTask(task);

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        await _handler.HandleAsync(
            new NextTaskCommand("KEEN-COBRA-66", adminId), CancellationToken.None);

        Assert.That(task.IsCompleted, Is.True,
            "The first uncompleted task must be marked as completed when advancing");
    }

    [Test]
    public async Task HandleAsync_WhenMoreTasksRemain_NotifiesRoomOfTaskCompletion()
    {
        var room  = CreateRoomWithAdmin(out var adminId);
        var task1 = TaskItem.Create(room.Id, "Story A", 1);
        var task2 = TaskItem.Create(room.Id, "Story B", 2);
        room.AddTask(task1);
        room.AddTask(task2);

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        await _handler.HandleAsync(
            new NextTaskCommand("KEEN-COBRA-66", adminId), CancellationToken.None);

        _notifierMock.Verify(
            n => n.NotifyTaskCompleted(It.IsAny<string>(), It.IsAny<TaskCompletedPayload>()),
            Times.Once, "When more tasks remain, NotifyTaskCompleted must be broadcast");
    }

    [Test]
    public async Task HandleAsync_WhenLastTaskIsCompleted_NotifiesRoomFinished()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        var task = TaskItem.Create(room.Id, "Final Story", 1);
        room.AddTask(task);

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        await _handler.HandleAsync(
            new NextTaskCommand("KEEN-COBRA-66", adminId), CancellationToken.None);

        _notifierMock.Verify(
            n => n.NotifyRoomFinished(It.IsAny<string>()),
            Times.Once, "When all tasks are done, the room-finished event must fire");
    }

    [Test]
    public async Task HandleAsync_WhenLastTaskIsCompleted_DoesNotNotifyTaskCompletion()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        var task = TaskItem.Create(room.Id, "Final Story", 1);
        room.AddTask(task);

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        await _handler.HandleAsync(
            new NextTaskCommand("KEEN-COBRA-66", adminId), CancellationToken.None);

        _notifierMock.Verify(
            n => n.NotifyTaskCompleted(It.IsAny<string>(), It.IsAny<TaskCompletedPayload>()),
            Times.Never, "NotifyTaskCompleted must not fire when the last task is completed");
    }

    [Test]
    public async Task HandleAsync_WhenNoTasksAtAll_SendsRoomResetNotification()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        // No tasks added to room

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        await _handler.HandleAsync(
            new NextTaskCommand("KEEN-COBRA-66", adminId), CancellationToken.None);

        _notifierMock.Verify(
            n => n.NotifyRoomReset(It.IsAny<string>(), It.IsAny<RoomResetPayload>()),
            Times.Once, "When there are no tasks at all, a room-reset notification must be sent");
    }

    [Test]
    public async Task HandleAsync_WhenUncompletedTaskExists_SavesTaskChanges()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        var task = TaskItem.Create(room.Id, "Story A", 1);
        room.AddTask(task);

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        await _handler.HandleAsync(
            new NextTaskCommand("KEEN-COBRA-66", adminId), CancellationToken.None);

        _tasksMock.Verify(t => t.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_AlwaysReturnsOk_WhenCallSucceeds()
    {
        var room = CreateRoomWithAdmin(out var adminId);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);

        var result = await _handler.HandleAsync(
            new NextTaskCommand("KEEN-COBRA-66", adminId), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Ok));
    }
}
