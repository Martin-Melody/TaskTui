using Terminal.Gui;
using TaskTui.Models;
using TaskTui.Services;

namespace TaskTui.UI;

public class DayScreen : Window
{
    private readonly ITaskStore _store;
    private readonly DateTime _date;

    private readonly ListView _hours;
    private readonly TaskListView _list;
    private readonly TaskListActions.HandlerSet _listActions;

    public DayScreen(ITaskStore store, DateTime date) : base($"Day — {date:dddd, dd MMM yyyy}")
    {
        _store = store;
        _date = date.Date;

        X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
        Border.BorderStyle = BorderStyle.None;

        // left gutter with 24 hours
        var hourLines = Enumerable.Range(0, 24).Select(h => $"{h:00}:00").ToList();
        _hours = new ListView(hourLines)
        {
            X = 0,
            Y = 0,
            Width = 8,
            Height = Dim.Fill(),
            CanFocus = true
        };

        // right: task list (same widget as elsewhere)
        _list = new TaskListView(_store, showFilterBar: false)
        {
            X = Pos.Right(_hours) + 1,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        Add(_hours, _list);

        // filter the list to today's tasks
        _list.SetFilter(t => t.Due.HasValue && t.Due.Value.Date == _date, _date.ToShortDateString());
        _list.Refresh();

        _listActions = TaskListActions.AttachHandlers(
            _list,
            _store,
            newItemFactory: CreateTaskForSelectedHour);

        // hour gutter keys
        _hours.KeyPress += e =>
        {
            var k = e.KeyEvent.Key;
            var ch = e.KeyEvent.KeyValue > 0 ? (char)e.KeyEvent.KeyValue : '\0';
            if (k == Key.CursorDown || ch == 'j') { _hours.SelectedItem = Math.Min(_hours.SelectedItem + 1, 23); e.Handled = true; return; }
            if (k == Key.CursorUp || ch == 'k') { _hours.SelectedItem = Math.Max(_hours.SelectedItem - 1, 0); e.Handled = true; return; }
            if (ch == 'a' || ch == 'A') { _listActions.Add(); e.Handled = true; return; }
            if (k == Key.Tab || k == Key.Enter || ch == 'l') { _list.FocusTable(); e.Handled = true; return; }
            if (k == Key.Esc) { Application.RequestStop(); e.Handled = true; return; }
        };

        // list keys: Esc closes the day screen
        _list.KeyPress += e =>
        {
            if (e.KeyEvent.Key == Key.Esc) { Application.RequestStop(); e.Handled = true; }
        };
    }

    private TaskItem? CreateTaskForSelectedHour()
    {
        int hour = Math.Clamp(_hours.SelectedItem, 0, 23);
        var task = new TaskItem
        {
            Due = _date,
            StartTime = new TimeOnly(hour, 0)
        };
        return TaskDialogs.ShowEditDialog(task) ? task : null;
    }
}

