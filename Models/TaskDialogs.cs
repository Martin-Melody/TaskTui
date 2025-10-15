using System.Globalization;
using Terminal.Gui;
using TaskTui.Models;

namespace TaskTui.UI;

public static class TaskDialogs
{
    public static TaskItem? ShowAddDialog()
    {
        var t = new TaskItem();
        return ShowEditor("Add Task", t) ? t : null;
    }

    public static bool ShowEditDialog(TaskItem task) =>
        ShowEditor("Edit Task", task);

    private static bool ShowEditor(string title, TaskItem model)
    {
        // widen & tall-ify for comfort
        var dlg = new Dialog(title, width: 70, height: 22);

        // Title
        var lblTitle = new Label("Title:") { X = 1, Y = 1 };
        var txtTitle = new TextField(model.Title ?? "")
        {
            X = 12,
            Y = Pos.Top(lblTitle),
            Width = 54
        };

        // Due + Done
        var lblDue = new Label("Due (YYYY-MM-DD):") { X = 1, Y = Pos.Bottom(lblTitle) + 1 };
        var txtDue = new TextField(model.Due?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"))
        {
            X = 22,
            Y = Pos.Top(lblDue),
            Width = 20
        };
        var chkDone = new CheckBox("Done")
        {
            X = Pos.Right(txtDue) + 3,
            Y = Pos.Top(lblDue),
            Checked = model.Done
        };

        // Start/End (HH:mm)
        var lblStartTime = new Label("Start (HH:mm):") { X = 1, Y = Pos.Bottom(lblDue) + 1 };
        var txtStartTime = new TextField(model.StartTime?.ToString("HH:mm") ?? "")
        {
            X = 22,
            Y = Pos.Top(lblStartTime),
            Width = 10
        };

        var lblEndTime = new Label("End (HH:mm):") { X = Pos.Right(txtStartTime) + 4, Y = Pos.Top(lblStartTime) };
        var txtEndTime = new TextField(model.EndTime?.ToString("HH:mm") ?? "")
        {
            X = Pos.Right(lblEndTime) + 1,
            Y = Pos.Top(lblStartTime),
            Width = 10
        };

        bool TryParseTime(string s, out TimeOnly t)
            => TimeOnly.TryParseExact(s, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out t);

        // Priority
        var lblPri = new Label("Priority:") { X = 1, Y = Pos.Bottom(lblStartTime) + 1 };
        var cboPri = new ComboBox()
        {
            X = 12,
            Y = Pos.Top(lblPri),
            Width = 20,
            Height = 4
        };
        cboPri.SetSource(Enum.GetNames<Priority>().ToList());
        cboPri.Text = model.Priority.ToString();

        // Tags
        var lblTags = new Label("Tags (comma):") { X = 1, Y = Pos.Bottom(lblPri) + 1 };
        var txtTags = new TextField(string.Join(",", model.Tags))
        {
            X = 12,
            Y = Pos.Top(lblTags),
            Width = 54
        };

        // Notes (taller)
        var lblNotes = new Label("Notes:") { X = 1, Y = Pos.Bottom(lblTags) + 1 };
        var txtNotes = new TextView()
        {
            X = 12,
            Y = Pos.Top(lblNotes),
            Width = 54,
            Height = 6,
            Text = model.Notes ?? ""
        };

        // Buttons (Dialog auto-places at bottom)
        var btnCancel = new Button("Cancel");
        var btnOk = new Button("Save", is_default: true);

        bool saved = false;

        btnCancel.Clicked += () => Application.RequestStop();
        btnOk.Clicked += () =>
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text.ToString()))
            {
                MessageBox.ErrorQuery("Validation", "Title is required.", "OK");
                return;
            }

            // Times (optional)
            TimeOnly? start = null, end = null;
            var sStart = txtStartTime.Text.ToString();
            var sEnd = txtEndTime.Text.ToString();

            if (!string.IsNullOrWhiteSpace(sStart))
            {
                if (!TryParseTime(sStart!, out var t)) { MessageBox.ErrorQuery("Validation", "Invalid start time (HH:mm).", "OK"); return; }
                start = t;
            }
            if (!string.IsNullOrWhiteSpace(sEnd))
            {
                if (!TryParseTime(sEnd!, out var t)) { MessageBox.ErrorQuery("Validation", "Invalid end time (HH:mm).", "OK"); return; }
                end = t;
            }
            if (start.HasValue && end.HasValue && end < start)
            {
                MessageBox.ErrorQuery("Validation", "End time must be after start time.", "OK");
                return;
            }

            // Save back to model
            model.StartTime = start;
            model.EndTime = end;
            model.Title = txtTitle.Text.ToString()!;
            model.Done = chkDone.Checked;

            if (DateTime.TryParse(txtDue.Text.ToString(), out var due))
                model.Due = due;
            else
                model.Due = null;

            if (Enum.TryParse<Priority>(cboPri.Text.ToString(), out var p))
                model.Priority = p;

            model.Tags = txtTags.Text.ToString()!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            model.Notes = txtNotes.Text.ToString();

            saved = true;
            Application.RequestStop();
        };

        dlg.Add(
            lblTitle, txtTitle,
            lblDue, txtDue, chkDone,
            lblStartTime, txtStartTime, lblEndTime, txtEndTime,
            lblPri, cboPri,
            lblTags, txtTags,
            lblNotes, txtNotes
        );

        dlg.AddButton(btnCancel);
        dlg.AddButton(btnOk);

        Application.Run(dlg);
        return saved;
    }
}

