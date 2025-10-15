using System.Data;
using Terminal.Gui;
using TaskTui.Models;
using TaskTui.Services;

namespace TaskTui.UI;

public class TaskListView : View
{
    public event Action? AddRequested;
    public event Action<TaskItem>? EditRequested;
    public event Action<TaskItem>? ToggleDoneRequested;
    public event Action<TaskItem>? DeleteRequested;

    private readonly ITaskStore _store;
    private readonly TableView _table;
    private readonly DataTable _tableData = new();
    private readonly Label _filterBar;

    private readonly List<DisplayRow> _rows = new();
    private readonly HashSet<Guid> _expanded = new();

    private Func<TaskItem, bool> _filter = _ => true;
    private string _filterLabel = "All";
    private readonly bool _showFilterBar;

    private record DisplayRow(TaskItem Item, bool IsHeader, string Text);

    public TaskListView(ITaskStore store, bool showFilterBar = true)
    {
        _store = store;
        _showFilterBar = showFilterBar;

        CanFocus = true;
        X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();

        _filterBar = new Label("Filter: All")
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            Visible = showFilterBar
        };
        Add(_filterBar);

        _tableData.Columns.Add("Tasks", typeof(string));
        _table = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = showFilterBar ? Dim.Fill() - 1 : Dim.Fill(),
            FullRowSelect = true,
            Table = _tableData
        };

        _table.ColorScheme = DarkTheme.Table;
        _table.Style.AlwaysShowHeaders = false;
        _table.Style.ShowVerticalCellLines = false;
        _table.Style.ShowVerticalHeaderLines = false;
        _table.Style.ShowHorizontalHeaderOverline = false;
        _table.Style.ShowHorizontalHeaderUnderline = false;

        _table.KeyPress += OnKeyPress;
        Add(_table);
        Refresh();
    }

    public void FocusTable() => _table.SetFocus();

    public TaskItem? SelectedItem()
    {
        if (_rows.Count == 0 || _table.SelectedRow < 0 || _table.SelectedRow >= _rows.Count) return null;
        return _rows[_table.SelectedRow].Item;
    }

    public void SetFilter(Func<TaskItem, bool> filter, string label)
    {
        _filter = filter ?? (_ => true);
        _filterLabel = string.IsNullOrWhiteSpace(label) ? "Custom" : label;
        if (_showFilterBar) _filterBar.Text = $"Filter: {_filterLabel}";
        _expanded.Clear();
        Refresh();
    }

    public void AdjustForStatusBar(bool visible)
    {
        if (!_showFilterBar) return;
        _filterBar.Y = visible ? Pos.AnchorEnd(2) : Pos.AnchorEnd(1);
        _table.Height = visible ? Dim.Fill() - 2 : Dim.Fill() - 1;
        SetNeedsDisplay();
    }

    public void Refresh()
    {
        _rows.Clear();
        _tableData.Rows.Clear();

        foreach (var t in _store.All().Where(_filter))
        {
            string timePrefix = "";
            if (t.StartTime.HasValue || t.EndTime.HasValue)
            {
                if (t.StartTime.HasValue && t.EndTime.HasValue)
                    timePrefix = $"{t.StartTime:HH\\:mm}–{t.EndTime:HH\\:mm} ";
                else if (t.StartTime.HasValue)
                    timePrefix = $"{t.StartTime:HH\\:mm} ";
                else
                    timePrefix = $"{t.EndTime:HH\\:mm} ";
            }

            var head = $"{(t.Done ? "[x]" : "[ ]")} {timePrefix}{t.Title}";
            _rows.Add(new DisplayRow(t, true, head));
            _tableData.Rows.Add(head);

            if (_expanded.Contains(t.Id))
            {
                void AddDetail(string label, string? val)
                {
                    if (string.IsNullOrWhiteSpace(val)) return;
                    var line = $"  - {label}: {val}";
                    _rows.Add(new DisplayRow(t, false, line));
                    _tableData.Rows.Add(line);
                }

                var tags = (t.Tags?.Count ?? 0) > 0 ? string.Join(", ", t.Tags) : null;
                AddDetail("Tags", tags);
                AddDetail("Priority", t.Priority.ToString());
                AddDetail("Notes", string.IsNullOrWhiteSpace(t.Notes) ? null : t.Notes);
                AddDetail("Date", t.Due?.ToString("yyyy-MM-dd"));

                var when = t.StartTime.HasValue || t.EndTime.HasValue
                    ? (t.StartTime, t.EndTime) switch
                    {
                        (not null, not null) => $"{t.StartTime:HH\\:mm}–{t.EndTime:HH\\:mm}",
                        (not null, null) => $"{t.StartTime:HH\\:mm}",
                        (null, not null) => $"{t.EndTime:HH\\:mm}",
                        _ => ""
                    }
                    : null;
                AddDetail("Time", when);
            }
        }
        _table.Update();
    }

    // ---- keys inside the widget ----
    private void OnKeyPress(KeyEventEventArgs args)
    {
        var k = args.KeyEvent.Key;
        var ch = args.KeyEvent.KeyValue > 0 ? (char)args.KeyEvent.KeyValue : '\0';

        // expand/collapse
        if (k == Key.Enter) { ToggleExpandSelected(); args.Handled = true; return; }

        // j/k or arrows — move between headers
        if (k == Key.CursorDown || ch == 'j') { MoveToHeader(+1); args.Handled = true; return; }
        if (k == Key.CursorUp || ch == 'k') { MoveToHeader(-1); args.Handled = true; return; }

        // g / G
        if (ch == 'g')
        {
            if (_rows.Count > 0) _table.SelectedRow = _rows.FindIndex(r => r.IsHeader);
            args.Handled = true; Refresh(); return;
        }
        if (ch == 'G' || ((k & Key.ShiftMask) != 0 && args.KeyEvent.KeyValue == 'g'))
        {
            if (_rows.Count > 0)
            {
                var last = _rows.FindLastIndex(r => r.IsHeader);
                if (last >= 0) _table.SelectedRow = last;
            }
            args.Handled = true; Refresh(); return;
        }

        // actions delegated to host
        var cur = SelectedItem();
        if (ch == 'a') { AddRequested?.Invoke(); args.Handled = true; return; }
        if (ch == 'e' && cur != null) { EditRequested?.Invoke(cur); args.Handled = true; return; }
        if ((k == Key.Space || ch == 'x') && cur != null) { ToggleDoneRequested?.Invoke(cur); args.Handled = true; return; }
        if (ch == 'd' && cur != null) { DeleteRequested?.Invoke(cur); args.Handled = true; return; }
    }

    public void ToggleExpandSelected()
    {
        if (_rows.Count == 0 || _table.SelectedRow < 0 || _table.SelectedRow >= _rows.Count) return;
        var row = _rows[_table.SelectedRow];
        var id = row.Item.Id;

        if (_expanded.Contains(id)) _expanded.Remove(id);
        else _expanded.Add(id);

        Refresh();
        var headerIdx = _rows.FindIndex(r => r.Item.Id == id && r.IsHeader);
        if (headerIdx >= 0) _table.SelectedRow = headerIdx;
    }

    private void MoveToHeader(int dir)
    {
        if (_rows.Count == 0) return;
        var i = _table.SelectedRow;
        if (dir > 0)
        {
            i = Math.Min(_rows.Count - 1, i + 1);
            while (i < _rows.Count && !_rows[i].IsHeader) i++;
            if (i < _rows.Count) _table.SelectedRow = i;
        }
        else
        {
            i = Math.Max(0, i - 1);
            while (i >= 0 && !_rows[i].IsHeader) i--;
            if (i >= 0) _table.SelectedRow = i;
        }
        Refresh();
    }
}

