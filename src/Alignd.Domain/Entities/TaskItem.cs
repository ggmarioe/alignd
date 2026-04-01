namespace Alignd.Domain.Entities;

public sealed class TaskItem
{
    public Guid    Id          { get; private set; } = Guid.NewGuid();
    public Guid    RoomId      { get; private set; }
    public string  Title       { get; private set; } = default!;
    public string? Description { get; private set; }
    public int     Order       { get; private set; }
    public bool    IsCompleted { get; private set; }

    private TaskItem() { }

    public static TaskItem Create(Guid roomId, string title, int order,
                                  string? description = null) => new()
    {
        RoomId      = roomId,
        Title       = title,
        Description = description,
        Order       = order,
        IsCompleted = false
    };

    public void Complete() => IsCompleted = true;
}
