// Services/TaskStore.cs
using TaskTui.Models;

namespace TaskTui.Services;

public interface ITaskStore
{
    IReadOnlyList<TaskItem> All();
    TaskItem Add(TaskItem t);
    void Update(TaskItem t);
    void Remove(Guid id);
}

public class InMemoryTaskStore : ITaskStore
{
    private readonly List<TaskItem> _items = new();
    public IReadOnlyList<TaskItem> All() => _items;
    public TaskItem Add(TaskItem t) { _items.Add(t); return t; }
    public void Update(TaskItem t) { /* nothing for in-mem */ }
    public void Remove(Guid id) { _items.RemoveAll(i => i.Id == id); }
}

