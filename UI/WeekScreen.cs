// UI/WeekScreen.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Terminal.Gui;
using TaskTui.Models;
using TaskTui.Services;

namespace TaskTui.UI;

public class WeekScreen : Window
{
    private readonly ITaskStore _store;
    private readonly DateTime _seed;
    private DateTime _monday;

    private readonly TableView _strip;
    private readonly DataTable _data = new();
    private readonly Dictionary<int, DateTime> _colDate = new();

    private readonly Label _title;
    private readonly FrameView _right;
    private readonly TaskListView _dayList;
    private readonly TaskListActions.HandlerSet _dayActions;

    private const int COLS = 7;
    private const int CELL_W = 14;

    public WeekScreen(ITaskStore store, DateTime seed) : base("Week")
    {
        _store = store;
        _seed = seed.Date;
        _monday = seed.Date.AddDays(-(((int)seed.DayOfWeek + 6) % 7)); // Monday

        X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill();
        Border.BorderStyle = BorderStyle.None;

        _title = new Label("") { X = 0, Y = 0, Width = Dim.Fill() };
        var legend = new Label("←/→ or h/l day • t today • a add • Enter focus list • d day • Esc close")
        {
            X = 0, Y = 1, Width = Dim.Fill()
        };
        Add(_title, legend);

        // Monday-first headers and one data row
        for (int i = 0; i < COLS; i++)
            _data.Columns.Add(PadCenter(
                CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[(i + 1) % 7],
                CELL_W), typeof(string));
        if (_data.Rows.Count == 0) _data.Rows.Add(_data.NewRow());

        _strip = new TableView
        {
            X = 0,
            Y = 2,
            Width = COLS * CELL_W,
            Height = 4,                     // <-- give enough space: header + underline + 1 row
            Table = _data,
            FullRowSelect = true,
            CanFocus = true
        };
        _strip.ColorScheme = DarkTheme.Table;
        _strip.Style.AlwaysShowHeaders = true;
        _strip.Style.ShowVerticalCellLines = true;
        _strip.Style.ShowHorizontalHeaderUnderline = true; // (set to false if you prefer Height=3)
        Add(_strip);

        _right = new FrameView("Day")
        {
            X = Pos.Right(_strip) + 2,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _dayList = new TaskListView(_store, showFilterBar: false);
        _right.Add(_dayList);
        Add(_right);

        // parity actions with main list
        _dayActions = TaskListActions.AttachHandlers(
            _dayList,
            _store,
            newItemFactory: CreateTaskForSelectedDate,
            afterChange: OnDayListChanged);

        _strip.KeyPress += OnStripKeyPress;
        _strip.SelectedCellChanged += _ => UpdateDayList();   // arrow/mouse changes

        PaintStrip(select: _seed);
    }

    private void OnStripKeyPress(KeyEventEventArgs e)
    {
        var k = e.KeyEvent.Key;
        var ch = e.KeyEvent.KeyValue > 0 ? (char)e.KeyEvent.KeyValue : '\0';

        if (k == Key.CursorLeft || ch == 'h') { MoveDay(-1); e.Handled = true; return; }
        if (k == Key.CursorRight|| ch == 'l') { MoveDay(+1); e.Handled = true; return; }
        if (ch == 't' || ch == 'T')           { PaintStrip(select: DateTime.Today); e.Handled = true; return; }

        if (ch == 'a' || ch == 'A')
        {
            _dayActions.Add();
            e.Handled = true; return;
        }

        if (k == Key.Enter) { _dayList.FocusTable(); e.Handled = true; return; }

        if (ch == 'd' || ch == 'D')
        {
            var d = SelectedDate() ?? DateTime.Today;
            Application.Run(new DayScreen(_store, d));
            PaintStrip(select: d);
            e.Handled = true; return;
        }

        if (k == Key.Esc) { Application.RequestStop(); e.Handled = true; return; }
    }

    private void MoveDay(int delta)
    {
        var d = SelectedDate() ?? _monday;
        PaintStrip(select: d.AddDays(delta));
    }

    private DateTime? SelectedDate()
    {
        if (_strip.SelectedColumn < 0 || _strip.SelectedColumn >= COLS) return null;
        return _colDate.TryGetValue(_strip.SelectedColumn, out var d) ? d : null;
    }

    private void PaintStrip(DateTime? select = null)
    {
        var target = select ?? _seed;
        _monday = target.AddDays(-(((int)target.DayOfWeek + 6) % 7));
        _title.Text = $"Week of {_monday:dd MMM yyyy}";

        _colDate.Clear();
        if (_data.Rows.Count == 0) _data.Rows.Add(_data.NewRow());

        for (int c = 0; c < COLS; c++)
        {
            var d = _monday.AddDays(c);
            _colDate[c] = d;

            int count = _store.All().Count(t => t.Due.HasValue && t.Due.Value.Date == d.Date);
            string badge = count == 0 ? "" : "  •";
            string dd = d.Day.ToString().PadLeft(2);
            string mark = d.Date == DateTime.Today ? "*" : " ";
            _data.Rows[0][c] = FitRightPad($"{mark} {dd}{badge}", CELL_W);
        }
        _strip.Update();

        // select desired day safely
        int sel = Math.Clamp((int)(target.Date - _monday).TotalDays, 0, 6);
        _strip.SelectedRow = 0;
        _strip.SelectedColumn = sel;

        // push date straight to the right pane (avoids timing issues)
        UpdateDayList(forced: target.Date);
    }

    // allow a forced date to avoid relying on selection timing
    private void UpdateDayList(DateTime? forced = null)
    {
        var d = forced ?? SelectedDate();
        _right.Title = d?.ToString("dddd, dd MMM yyyy") ?? "Day";

        if (d == null)
        {
            _dayList.SetFilter(_ => false, "No day");
            _dayList.Refresh();
            return;
        }

        _dayList.SetFilter(t => t.Due.HasValue && t.Due.Value.Date == d.Value.Date,
                           d.Value.ToShortDateString());
        _dayList.Refresh();
    }

    private TaskItem? CreateTaskForSelectedDate()
    {
        var date = SelectedDate();
        if (date == null) return null;
        var task = new TaskItem { Due = date.Value.Date };
        return TaskDialogs.ShowEditDialog(task) ? task : null;
    }

    private void OnDayListChanged(TaskListActions.ChangeKind kind, TaskItem task)
    {
        if (kind == TaskListActions.ChangeKind.Added)
        {
            DateTime? target = task.Due?.Date ?? SelectedDate()?.Date;
            PaintStrip(select: target);
        }
        else
        {
            PaintStrip();
        }
    }

    private static string PadCenter(string s, int width)
    {
        if (s.Length >= width) return s[..width];
        int pad = width - s.Length; int left = pad / 2; int right = pad - left;
        return new string(' ', left) + s + new string(' ', right);
    }
    private static string FitRightPad(string s, int width) =>
        s.Length >= width ? s[..width] : s.PadRight(width);
}

