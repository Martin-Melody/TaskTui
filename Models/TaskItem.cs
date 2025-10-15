// Models/TaskItem.cs
namespace TaskTui.Models;

public enum Priority { Low, Medium, High }

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool Done { get; set; }
    public string Title { get; set; } = "";
    public DateTime? Due { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Today;
    public Priority Priority { get; set; } = Priority.Medium;
    public string? Notes { get; set; }
    public List<string> Tags { get; set; } = new();

    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

}

