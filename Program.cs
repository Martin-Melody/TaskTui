using Terminal.Gui;
using TaskTui.Services;
using TaskTui.UI;
using TaskTui.Models;

Application.Init();
try
{
    DarkTheme.ApplyToApp();
    var top = Application.Top;

    var store = new JsonTaskStore(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".tasktui", "tasks.json"));

    // --- main task list widget (reusable view) ---
    var list = new TaskListView(store);
    top.Add(list);

    // Calendar (lazy)
    CalendarScreen? cal = null;
    bool isCalendar = false;

    // ----- Actions (single source of truth) -----
    void DoAdd()
    {
        var t = TaskDialogs.ShowAddDialog();
        if (t != null) { store.Add(t); list.Refresh(); }
    }
    void DoEdit(TaskItem t)
    {
        if (TaskDialogs.ShowEditDialog(t)) { store.Update(t); list.Refresh(); }
    }
    void DoToggle(TaskItem t)
    {
        t.Done = !t.Done; store.Update(t); list.Refresh();
    }
    void DoDelete(TaskItem t)
    {
        if (MessageBox.Query("Delete", $"Delete '{t.Title}'?", "Yes", "No") == 0)
        { store.Remove(t.Id); list.Refresh(); }
    }

    // wire the widget to the actions
    list.AddRequested += DoAdd;
    list.EditRequested += DoEdit;
    list.ToggleDoneRequested += DoToggle;
    list.DeleteRequested += DoDelete;

    // ----- View switches -----
    void ShowList()
    {
        if (cal != null && top.Subviews.Contains(cal)) top.Remove(cal);
        if (!top.Subviews.Contains(list)) top.Add(list);
        isCalendar = false;
        top.SetNeedsDisplay();
    }

    void ShowCalendar()
    {
        if (cal == null) cal = new CalendarScreen(store);
        if (top.Subviews.Contains(list)) top.Remove(list);
        if (!top.Subviews.Contains(cal)) top.Add(cal);
        isCalendar = true;
        top.SetNeedsDisplay();
    }

    void ToggleView() { if (isCalendar) ShowList(); else ShowCalendar(); }

    // ----- Filters (same API, but call list.SetFilter) -----
    void setAll() => list.SetFilter(Filters.All, "All");
    void setOpen() => list.SetFilter(Filters.Open, "Open");
    void setDone() => list.SetFilter(Filters.Done, "Done");
    void setToday() => list.SetFilter(Filters.Today, "Today");
    void setDueToday() => list.SetFilter(Filters.DueToday, "Due Today");
    void setOverdue() => list.SetFilter(Filters.Overdue, "Overdue");
    void setDueInNDays()
    {
        if (AskNumber("Due in N days", "Enter number of days:", out var n))
            list.SetFilter(Filters.DueWithinDays(n), $"Due ≤ {n} days");
    }
    void searchTitle()
    {
        if (AskText("Search Title", "Text to find:", out var q) && !string.IsNullOrWhiteSpace(q))
            list.SetFilter(Filters.TitleContains(q), $"Title contains \"{q}\"");
    }
    void searchTag()
    {
        if (AskText("Search Tag", "Tag text:", out var q) && !string.IsNullOrWhiteSpace(q))
            list.SetFilter(Filters.TagContains(q), $"Tag contains \"{q}\"");
    }

    // ----- prompts -----
    bool AskText(string title, string prompt, out string value)
    {
        var dlg = new Dialog(title, 60, 9);
        var lbl = new Label(prompt) { X = 1, Y = 1 };
        var txt = new TextField("") { X = 1, Y = 2, Width = 56 };
        var cancel = new Button("Cancel");
        var ok = new Button("OK", is_default: true);

        string result = "";
        bool okPressed = false;

        cancel.Clicked += () => Application.RequestStop();
        ok.Clicked += () => { result = txt.Text.ToString() ?? ""; okPressed = true; Application.RequestStop(); };

        dlg.Add(lbl, txt); dlg.AddButton(cancel); dlg.AddButton(ok);
        Application.Run(dlg);

        value = result;
        return okPressed;
    }

    bool AskNumber(string title, string prompt, out int number)
    {
        number = 0;
        if (!AskText(title, prompt, out var s)) return false;
        return int.TryParse(s, out number);
    }

    // ----- Menu -----
    var menu = new MenuBar(new MenuBarItem[] {
        new("_File", new MenuItem[]{ new("_Quit\tq", "", () => Application.RequestStop()) }),
        new("_Task", new MenuItem[]{
            new("_Add\ta", "", () => DoAdd()),
            new("_Expand/Collapse\tEnter", "", () => list.ToggleExpandSelected()),
            new("_Edit\te", "", () => { var cur = list.SelectedItem(); if (cur!=null) DoEdit(cur); }),
            new("_Toggle Done\tSpace", "", () => { var cur = list.SelectedItem(); if (cur!=null) DoToggle(cur); }),
            new("_Delete\td", "", () => { var cur = list.SelectedItem(); if (cur!=null) DoDelete(cur); })
        }),
        new("_Filter", new MenuItem[]{
            new("_All\tA", "", () => setAll()),
            new("_Open\to", "", () => setOpen()),
            new("_Done\tD", "", () => setDone()),
            new("_Today\tt", "", () => setToday()),
            new("_DueToday\tT", "", () => setDueToday()),
            new("_Overdue\tO", "", () => setOverdue()),
            new("_Due in N days\tn", "", () => setDueInNDays()),
            new("_Title contains\t/", "", () => searchTitle()),
            new("_Tag contains\t#", "", () => searchTag()),
        }),
        new("_View", new MenuItem[]{
            new("_Task List\tl", "", () => ShowList()),
            new("_Calendar\tc", "", () => ShowCalendar()),
            new("_Toggle\tv", "", () => ToggleView())
        })
    });
    top.Add(menu);

    // ----- Status bar -----
    var status = new StatusBar(new StatusItem[] {
        new StatusItem((Key)'a', "~a~ Add", () => DoAdd()),
        new StatusItem(Key.Enter, "~Enter~ Expand/Collapse", () => list.ToggleExpandSelected()),
        new StatusItem((Key)'e', "~e~ Edit", () => { var cur = list.SelectedItem(); if (cur!=null) DoEdit(cur); }),
        new StatusItem(Key.Space, "~Space~ Toggle Completed", () => { var cur = list.SelectedItem(); if (cur!=null) DoToggle(cur); }),
        new StatusItem((Key)'d', "~d~ Delete", () => { var cur = list.SelectedItem(); if (cur!=null) DoDelete(cur); }),
        new StatusItem((Key)'A', "~A~ All", () => setAll()),
        new StatusItem((Key)'o', "~o~ Open", () => setOpen()),
        new StatusItem((Key)'D', "~D~ Done", () => setDone()),
        new StatusItem((Key)'t', "~t~ Today", () => setToday()),
        new StatusItem((Key)'T', "~T~ Due Today", () => setDueToday()),
        new StatusItem((Key)'O', "~O~ Overdue", () => setOverdue()),
        new StatusItem((Key)'n', "~n~ Due ≤ N", () => setDueInNDays()),
        new StatusItem((Key)'/', "~/~ Title Search", () => searchTitle()),
        new StatusItem((Key)'#', "~#~ Tag Search", () => searchTag()),
        new StatusItem((Key)'v', "~v~ Toggle View", () => ToggleView()),
        new StatusItem((Key)'c', "~c~ Calendar", () => ShowCalendar()),
        new StatusItem((Key)'l', "~l~ Task List", () => ShowList()),
        new StatusItem((Key)'q', "~q~ Quit", () => Application.RequestStop())
    })
    { Visible = false };
    top.Add(status);

    // ----- Global keys -----
    top.KeyPress += e =>
    {
        var ch = e.KeyEvent.KeyValue > 0 ? (char)e.KeyEvent.KeyValue : '\0';

        if (ch == 'v') { ToggleView(); e.Handled = true; return; }
        if (ch == 'c') { ShowCalendar(); e.Handled = true; return; }
        if (ch == 'l') { ShowList(); e.Handled = true; return; }

        if (e.KeyEvent.Key == (Key)'?')
        {
            status.Visible = !status.Visible;
            if (!isCalendar) list.AdjustForStatusBar(status.Visible);
            e.Handled = true;
            top.SetNeedsDisplay();
            return;
        }

        if (ch == '/') { searchTitle(); e.Handled = true; return; }
        if (ch == '#') { searchTag(); e.Handled = true; return; }
        if (ch == 't') { setToday(); e.Handled = true; return; }
        if (ch == 'T') { setDueToday(); e.Handled = true; return; }
        if (ch == 'o') { setOpen(); e.Handled = true; return; }
        if (ch == 'O') { setOverdue(); e.Handled = true; return; }
        if (ch == 'A') { setAll(); e.Handled = true; return; }
        if (ch == 'n') { setDueInNDays(); e.Handled = true; return; }
    };

    Application.Run();
}
finally { Application.Shutdown(); }

