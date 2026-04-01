using Alignd.Domain.Entities;

namespace Alignd.Tests.Domain;

[TestFixture]
public sealed class TaskItemTests
{
    [Test]
    public void Create_SetsTitle_FromProvidedValue()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Implement login", 1);

        Assert.That(task.Title, Is.EqualTo("Implement login"));
    }

    [Test]
    public void Create_SetsOrder_FromProvidedValue()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Task A", 3);

        Assert.That(task.Order, Is.EqualTo(3));
    }

    [Test]
    public void Create_SetsRoomId_FromProvidedValue()
    {
        var roomId = Guid.NewGuid();

        var task = TaskItem.Create(roomId, "Task B", 1);

        Assert.That(task.RoomId, Is.EqualTo(roomId));
    }

    [Test]
    public void Create_WithNullDescription_SetsDescriptionToNull()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Task C", 1);

        Assert.That(task.Description, Is.Null,
            "When no description is provided it defaults to null");
    }

    [Test]
    public void Create_WithExplicitDescription_SetsDescription()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Task D", 1, "Some description");

        Assert.That(task.Description, Is.EqualTo("Some description"));
    }

    [Test]
    public void Create_NewTask_IsNotCompleted()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Task E", 1);

        Assert.That(task.IsCompleted, Is.False,
            "A newly created task must not be marked as completed");
    }

    [Test]
    public void Create_AssignsNewGuid_AsId()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Task F", 1);

        Assert.That(task.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Complete_MarksTask_AsCompleted()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Task G", 1);

        task.Complete();

        Assert.That(task.IsCompleted, Is.True,
            "Complete() must mark the task as completed");
    }

    [Test]
    public void Complete_CalledTwice_RemainsCompleted()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Task H", 1);

        task.Complete();
        task.Complete();

        Assert.That(task.IsCompleted, Is.True,
            "Calling Complete() more than once must not change the state");
    }
}
