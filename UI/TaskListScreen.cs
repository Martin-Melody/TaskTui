using System.Data;
using Terminal.Gui;
using TaskTui.Models;
using TaskTui.Services;

namespace TaskTui.UI;

public class TaskListScreen : Window
{
    private readonly ITaskStore _store;
    private readonly TableView _table;
    private readonly DataTable _tableData = new();

    private readonly List<DisplayRow> _rows = new();
    private readonly HashSet<Guid> _expanded = new();

    private Func<TaskItem, bool> _filter = _ => true;
    private string _filterLabel = "Today";
    private readonly Label _filterBar;

    private record DisplayRow(TaskItem Item, bool IsHeader, string Text);

    public TaskListScreen(ITaskStore store) : base("")
    {
        _store = store;

        Border.BorderStyle = BorderStyle.None;
        Border.Effect3D = false;

        X = 0; Y = 1;
        Width = Dim.Fill(); Height = Dim.Fill();

        // Bottom filter bar
        _filterBar = new Label("Filter: Today")
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill() - 1,
            Height = 1
        };
        Add(_filterBar);

        // Single column table above the filter bar
        _tableData.Columns.Add("Tasks", typeof(string));

        _table = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            Table = _tableData,

        };


        _table.ColorScheme = DarkTheme.Table;
        _table.Style.AlwaysShowHeaders = false;
        _table.Style.ShowVerticalCellLines = false;
        _table.Style.ShowVerticalHeaderLines = false;
        _table.Style.ShowHorizontalHeaderOverline = false;
        _table.Style.ShowHorizontalHeaderUnderline = false;

        // replace/extend your _table.KeyPress with this unified nav
        _table.KeyPress += args =>
        {
            var k = args.KeyEvent.Key;

            // toggle expand
            if (k == Key.Enter) { ToggleExpandSelected(); args.Handled = true; return; }

            // ↓ / j : next header
            if (k == Key.CursorDown || k == (Key)'j') { MoveToHeader(+1); args.Handled = true; return; }

            // ↑ / k : prev header
            if (k == Key.CursorUp || k == (Key)'k') { MoveToHeader(-1); args.Handled = true; return; }

            // g -> top (first header)
            if (k == (Key)'g')
            {
                if (_rows.Count > 0) _table.SelectedRow = _rows.FindIndex(r => r.IsHeader); args.Handled = true;
                Refresh(); return;
            }

            // G -> bottom (last header) — handles Shift+g too
            if (k == (Key)'G' || ((k & Key.ShiftMask) != 0 && args.KeyEvent.KeyValue == 'g'))
            {
                if (_rows.Count > 0)
                {
                    var last = _rows.FindLastIndex(r => r.IsHeader);
                    if (last >= 0) _table.SelectedRow = last;
                }
                args.Handled = true;
                Refresh();
                return;
            }
        };



        Add(_table);
        Refresh();
    }

    // Call this if you toggle a StatusBar at the app level
    public void AdjustForStatusBar(bool statusVisible)
    {
        _filterBar.Y = statusVisible ? Pos.AnchorEnd(2) : Pos.AnchorEnd(1);
        _table.Height = statusVisible ? Dim.Fill() - 2 : Dim.Fill() - 1;
        SetNeedsDisplay();
    }

    public TaskItem? SelectedItem()
    {
        if (_rows.Count == 0 || _table.SelectedRow < 0 || _table.SelectedRow >= _rows.Count) return null;
        return _rows[_table.SelectedRow].Item;
    }

    public void SetFilter(Func<TaskItem, bool> filter, string label)
    {
        _filter = filter ?? (_ => true);
        _filterLabel = string.IsNullOrWhiteSpace(label) ? "Custom" : label;
        _filterBar.Text = $"Filter: {_filterLabel}";
        _expanded.Clear(); // optional: collapse on filter change
        Refresh();
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

    private void ExpandSelected()
    {
        if (_rows.Count == 0 || _table.SelectedRow < 0 || _table.SelectedRow >= _rows.Count) return;
        var row = _rows[_table.SelectedRow];
        var id = row.Item.Id;
        if (_expanded.Contains(id)) return;

        _expanded.Add(id);
        Refresh();
        var headerIdx = _rows.FindIndex(r => r.Item.Id == id && r.IsHeader);
        if (headerIdx >= 0) _table.SelectedRow = headerIdx;
    }

    private void CollapseSelected()
    {
        if (_rows.Count == 0 || _table.SelectedRow < 0 || _table.SelectedRow >= _rows.Count) return;
        var row = _rows[_table.SelectedRow];
        var id = row.Item.Id;
        if (!_expanded.Contains(id)) return;

        _expanded.Remove(id);
        Refresh();
        var headerIdx = _rows.FindIndex(r => r.Item.Id == id && r.IsHeader);
        if (headerIdx >= 0) _table.SelectedRow = headerIdx;
    }

    // add this helper inside TaskListScreen
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


            var head = $"{(t.Done ? "[x]" : "[ ]")} {timePrefix} {t.Title}";
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
}

