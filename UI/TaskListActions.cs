using Terminal.Gui;
using TaskTui.Models;
using TaskTui.Services;

namespace TaskTui.UI;

public static class TaskListActions
{
    public enum ChangeKind
    {
        Added,
        Edited,
        Toggled,
        Deleted
    }

    public sealed class HandlerSet
    {
        public HandlerSet(Action add, Action<TaskItem> edit, Action<TaskItem> toggleDone, Action<TaskItem> delete)
        {
            Add = add;
            Edit = edit;
            ToggleDone = toggleDone;
            Delete = delete;
        }

        public Action Add { get; }
        public Action<TaskItem> Edit { get; }
        public Action<TaskItem> ToggleDone { get; }
        public Action<TaskItem> Delete { get; }
    }

    public static HandlerSet AttachHandlers(
        TaskListView view,
        ITaskStore store,
        Func<TaskItem?>? newItemFactory = null,
        Func<TaskItem, bool>? editHandler = null,
        Action<ChangeKind, TaskItem>? afterChange = null)
    {
        newItemFactory ??= TaskDialogs.ShowAddDialog;
        editHandler ??= TaskDialogs.ShowEditDialog;

        void InvokeAfter(ChangeKind kind, TaskItem task)
        {
            view.Refresh();
            afterChange?.Invoke(kind, task);
        }

        void Add()
        {
            var item = newItemFactory();
            if (item == null) return;
            store.Add(item);
            InvokeAfter(ChangeKind.Added, item);
        }

        void Edit(TaskItem task)
        {
            if (!editHandler(task)) return;
            store.Update(task);
            InvokeAfter(ChangeKind.Edited, task);
        }

        void Toggle(TaskItem task)
        {
            task.Done = !task.Done;
            store.Update(task);
            InvokeAfter(ChangeKind.Toggled, task);
        }

        void Delete(TaskItem task)
        {
            if (MessageBox.Query("Delete", $"Delete '{task.Title}'?", "Yes", "No") != 0) return;
            store.Remove(task.Id);
            InvokeAfter(ChangeKind.Deleted, task);
        }

        view.AddRequested += Add;
        view.EditRequested += Edit;
        view.ToggleDoneRequested += Toggle;
        view.DeleteRequested += Delete;

        return new HandlerSet(Add, Edit, Toggle, Delete);
    }
}

