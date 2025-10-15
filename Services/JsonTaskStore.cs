// Services/JsonTaskStore.cs
using System.Text.Json;
using TaskTui.Models;

namespace TaskTui.Services;

public class JsonTaskStore : ITaskStore
{
    private readonly string _path;
    private readonly List<TaskItem> _items;

    public JsonTaskStore(string path)
    {
        _path = path;
        _items = File.Exists(_path)
            ? JsonSerializer.Deserialize<List<TaskItem>>(File.ReadAllText(_path)) ?? new()
            : new();
    }

    public IReadOnlyList<TaskItem> All() => _items;

    public TaskItem Add(TaskItem t) { _items.Add(t); Save(); return t; }
    public void Update(TaskItem t) { Save(); }
    public void Remove(Guid id) { _items.RemoveAll(i => i.Id == id); Save(); }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, json);
    }
}

