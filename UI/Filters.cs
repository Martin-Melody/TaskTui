using TaskTui.Models;

namespace TaskTui.UI;

public static class Filters
{
    public static Func<TaskItem, bool> All => _ => true;
    public static Func<TaskItem, bool> Open => t => !t.Done;
    public static Func<TaskItem, bool> Done => t => t.Done;
    public static Func<TaskItem, bool> Today => t => t.CreatedAt.Date == DateTime.Today;
    public static Func<TaskItem, bool> DueToday => t => t.Due.HasValue && t.Due.Value.Date == DateTime.Today;
    public static Func<TaskItem, bool> Overdue => t => t.Due.HasValue && t.Due.Value.Date < DateTime.Today && !t.Done;
    public static Func<TaskItem, bool> DueWithinDays(int n)
        => t => t.Due.HasValue && t.Due.Value.Date <= DateTime.Today.AddDays(n);

    public static Func<TaskItem, bool> TitleContains(string q)
        => t => !string.IsNullOrWhiteSpace(q) &&
                (t.Title?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;

    public static Func<TaskItem, bool> TagContains(string q)
        => t => !string.IsNullOrWhiteSpace(q) &&
                (t.Tags?.Any(tag => tag.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ?? false);
}

