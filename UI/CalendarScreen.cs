using System.Data;
using System.Globalization;
using Terminal.Gui;
using TaskTui.Models;
using TaskTui.Services;

namespace TaskTui.UI;

public class CalendarScreen : Window
{
    private readonly ITaskStore _store;

    private readonly TableView _grid;
    private readonly DataTable _gridData = new();
    private readonly Dictionary<(int r, int c), DateTime> _cellDate = new();

    private readonly Label _monthLabel;
    private readonly FrameView _right;         // show selected day in title
    private readonly TaskListView _dayList;    // <-- REUSES your unified widget

    private DateTime _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    // --- sizing: uniform cells ---
    private const int COLS = 7;
    private const int ROWS = 6;
    private const int CELL_W = 11;
    private const int GRID_W = COLS * CELL_W;

    // --- markers ---
    private const string SELECT_L = "[";
    private const string SELECT_R = "]";
    private const string TODAY_MARK = "*";

    public CalendarScreen(ITaskStore store) : base("")
    {
        _store = store;

        Border.BorderStyle = BorderStyle.None;
        Border.Effect3D = false;

        X = 0; Y = 1;  // leave space for MenuBar
        Width = Dim.Fill(); Height = Dim.Fill();

        // Header: month + legend
        _monthLabel = new Label("") { X = 0, Y = 0, Width = 30 };
        var legend = new Label("←/→/↑/↓ or h/j/k/l move  •  </> month (PgUp/PgDn)  •  t today  •  a add  •  Enter day / Esc back")
        {
            X = Pos.Right(_monthLabel) + 2,
            Y = 0,
            Width = Dim.Fill()
        };
        Add(_monthLabel, legend);

        // Calendar grid headers (padded)
        _gridData.Columns.AddRange(new[]
        {
            new DataColumn(PadCenter("Mon", CELL_W), typeof(string)),
            new DataColumn(PadCenter("Tue", CELL_W), typeof(string)),
            new DataColumn(PadCenter("Wed", CELL_W), typeof(string)),
            new DataColumn(PadCenter("Thu", CELL_W), typeof(string)),
            new DataColumn(PadCenter("Fri", CELL_W), typeof(string)),
            new DataColumn(PadCenter("Sat", CELL_W), typeof(string)),
            new DataColumn(PadCenter("Sun", CELL_W), typeof(string)),
        });

        _grid = new TableView
        {
            X = 0,
            Y = 1,
            Width = GRID_W,
            Height = Dim.Fill(),
            FullRowSelect = true,
            Table = _gridData,
            CanFocus = true,
        };
        _grid.ColorScheme = DarkTheme.Table;
        _grid.Style.AlwaysShowHeaders = true;
        _grid.Style.ShowVerticalHeaderLines = false;
        _grid.Style.ShowHorizontalHeaderOverline = false;
        _grid.Style.ShowHorizontalHeaderUnderline = true;
        _grid.Style.ShowVerticalCellLines = true;

        // Right side = reusable task list (no filter bar)
        _right = new FrameView("Day")
        {
            X = Pos.Right(_grid) + 2,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        _dayList = new TaskListView(_store, showFilterBar: false);
        _right.Add(_dayList);

        Add(_grid, _right);

        // ---- Wire up grid events ----
        _grid.KeyPress += OnGridKeyPress;
        _grid.SelectedCellChanged += _ =>
        {
            UpdateDayPanel();
            RepaintCellsWithSelection(SelectedDate());
        };

        // ---- Day list actions (parity with main list) ----
        _dayList.AddRequested += () =>
        {
            var d = SelectedDate();
            if (d == null) return;
            var item = new TaskItem { Due = d.Value.Date };
            if (TaskDialogs.ShowEditDialog(item))
            {
                _store.Add(item);
                _dayList.Refresh();
                RepaintCellsWithSelection(d);
            }
        };
        _dayList.EditRequested += (t) =>
        {
            if (TaskDialogs.ShowEditDialog(t))
            {
                _store.Update(t);
                _dayList.Refresh();
                RepaintCellsWithSelection(SelectedDate());
            }
        };
        _dayList.ToggleDoneRequested += (t) =>
        {
            t.Done = !t.Done;
            _store.Update(t);
            _dayList.Refresh();
            RepaintCellsWithSelection(SelectedDate());
        };
        _dayList.DeleteRequested += (t) =>
        {
            if (MessageBox.Query("Delete", $"Delete '{t.Title}'?", "Yes", "No") != 0) return;
            _store.Remove(t.Id);
            _dayList.Refresh();
            RepaintCellsWithSelection(SelectedDate());
        };

        // Esc inside the day list returns focus to the grid
        _dayList.KeyPress += e =>
        {
            if (e.KeyEvent.Key == Key.Esc)
            {
                _grid.SetFocus();
                e.Handled = true;
            }
        };

        BuildMonth();
    }

    // ---------- grid navigation helpers ----------
    private void MoveDays(int deltaDays)
    {
        var date = SelectedDate() ?? _month;
        var target = date.AddDays(deltaDays);
        SelectDate(target);
    }

    private void SelectDate(DateTime target)
    {
        if (target.Year != _month.Year || target.Month != _month.Month)
        {
            _month = new DateTime(target.Year, target.Month, 1);
            BuildMonth(selectDate: target);
            return;
        }

        foreach (var kv in _cellDate)
        {
            if (kv.Value.Date == target.Date)
            {
                _grid.SelectedRow = kv.Key.r;
                _grid.SelectedColumn = kv.Key.c;
                try { _grid.EnsureSelectedCellIsVisible(); } catch { }
                _grid.SetNeedsDisplay();
                UpdateDayPanel();
                RepaintCellsWithSelection(target);
                return;
            }
        }
    }

    // ---------- GRID key handling ----------
    private void OnGridKeyPress(KeyEventEventArgs args)
    {
        var k = args.KeyEvent.Key;
        var ch = args.KeyEvent.KeyValue > 0 ? (char)args.KeyEvent.KeyValue : '\0';

        if (ch == '<' || k == Key.PageUp) { _month = _month.AddMonths(-1); BuildMonth(); args.Handled = true; return; }
        if (ch == '>' || k == Key.PageDown) { _month = _month.AddMonths(1); BuildMonth(); args.Handled = true; return; }
        if (ch == 't' || ch == 'T') { _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); BuildMonth(selectDate: DateTime.Today); args.Handled = true; return; }

        if (ch == 'w' || ch == 'W')
        {
            var d = SelectedDate() ?? DateTime.Today;
            Application.Run(new WeekScreen(_store, d));
            UpdateDayPanel();
            RepaintCellsWithSelection(SelectedDate());
            args.Handled = true;
        }

        if (ch == 'd' || ch == 'D')
        {
            var d = SelectedDate() ?? DateTime.Today;
            Application.Run(new DayScreen(_store, d));
            UpdateDayPanel();
            RepaintCellsWithSelection(SelectedDate());
            args.Handled = true;
        }

        if (ch == 'a' || ch == 'A')
        {
            var date = SelectedDate();
            if (date != null)
            {
                var item = new TaskItem { Due = date.Value.Date };
                if (TaskDialogs.ShowEditDialog(item))
                {
                    _store.Add(item);
                    BuildMonth(selectDate: date.Value);
                }
            }
            args.Handled = true; return;
        }

        // move by day/week
        if (k == Key.CursorLeft || ch == 'h') { MoveDays(-1); args.Handled = true; return; }
        if (k == Key.CursorRight || ch == 'l') { MoveDays(+1); args.Handled = true; return; }
        if (k == Key.CursorUp || ch == 'k') { MoveDays(-7); args.Handled = true; return; }
        if (k == Key.CursorDown || ch == 'j') { MoveDays(+7); args.Handled = true; return; }

        // Enter -> focus the day list (its inner TableView)
        if (k == Key.Enter)
        {
            _dayList.FocusTable();
            args.Handled = true;
            return;
        }
    }

    // ---------- build & paint ----------
    private void BuildMonth(DateTime? selectDate = null)
    {
        _monthLabel.Text = _month.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

        _gridData.Rows.Clear();
        _cellDate.Clear();

        var first = _month;
        int delta = ((int)first.DayOfWeek + 6) % 7; // Sun=0..Sat=6 -> Mon=0..Sun=6
        var start = first.AddDays(-delta);

        for (int r = 0; r < ROWS; r++)
        {
            var row = _gridData.NewRow();
            for (int c = 0; c < COLS; c++)
            {
                var date = start.AddDays(r * 7 + c);
                _cellDate[(r, c)] = date;
                row[c] = RenderCell(date, isSelected: false);
            }
            _gridData.Rows.Add(row);
        }

        _grid.Update();

        var target = selectDate ?? DateTime.Today;
        if (target.Month != _month.Month || target.Year != _month.Year) target = _month;

        foreach (var kv in _cellDate)
        {
            if (kv.Value.Date == target.Date)
            {
                _grid.SelectedRow = kv.Key.r;
                _grid.SelectedColumn = kv.Key.c;
                try { _grid.EnsureSelectedCellIsVisible(); } catch { }
                break;
            }
        }

        UpdateDayPanel();
        RepaintCellsWithSelection(target);
        SetNeedsDisplay();
    }

    private DateTime? SelectedDate()
    {
        if (_grid.SelectedRow < 0 || _grid.SelectedColumn < 0) return null;
        return _cellDate.TryGetValue((_grid.SelectedRow, _grid.SelectedColumn), out var d) ? d : null;
    }

    private void UpdateDayPanel()
    {
        var date = SelectedDate();
        if (date is null)
        {
            _right.Title = "Day";
            _dayList.SetFilter(_ => false, "No day");
            _dayList.Refresh();
            return;
        }

        _right.Title = date.Value.ToString("dddd, dd MMM yyyy");
        _dayList.SetFilter(t => t.Due.HasValue && t.Due.Value.Date == date.Value.Date,
                           date.Value.ToString("d"));
        _dayList.Refresh();
    }

    // ---------- cell rendering ----------
    private string RenderCell(DateTime date, bool isSelected)
    {
        int count = _store.All().Count(t => t.Due.HasValue && t.Due.Value.Date == date.Date);
        string badge = count == 0 ? "" : "  •";

        string dd = date.Day.ToString().PadLeft(2);
        dd = isSelected ? $"{SELECT_L}{dd}{SELECT_R}" : $" {dd}";

        string mark = date.Date == DateTime.Today ? TODAY_MARK : " ";

        return FitRightPad($"{mark}{dd}{badge}", CELL_W);
    }

    private void RepaintCellsWithSelection(DateTime? selected)
    {
        var sel = selected?.Date;
        for (int r = 0; r < _gridData.Rows.Count; r++)
            for (int c = 0; c < _gridData.Columns.Count; c++)
            {
                var date = _cellDate[(r, c)];
                bool isSel = sel.HasValue && date.Date == sel.Value;
                _gridData.Rows[r][c] = RenderCell(date, isSel);
            }
        _grid.Update();
    }

    // --- pad helpers ---
    private static string PadCenter(string s, int width)
    {
        if (s.Length >= width) return s[..width];
        int pad = width - s.Length;
        int left = pad / 2;
        int right = pad - left;
        return new string(' ', left) + s + new string(' ', right);
    }

    private static string FitRightPad(string s, int width)
    {
        if (s.Length >= width) return s[..width];
        return s.PadRight(width);
    }
}

