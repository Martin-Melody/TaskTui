using Terminal.Gui;

namespace TaskTui;

public static class AddScreens
{
    public static void ShowAddWindow(Toplevel top, Label mainLabel)
    {
        var addWin = new Window("Add") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };

        var firstNameLabel = new Label("First Name") { X = 2, Y = 2 };
        var firstNameInput = new TextField("") { X = Pos.Right(firstNameLabel) + 1, Y = firstNameLabel.Y, Width = 30 };
        var accept = new Button("Accept") { X = Pos.Center(), Y = 6 };

        accept.Clicked += () =>
        {
            MessageBox.Query("Saved", $"First Name: {firstNameInput.Text}", "OK");
            mainLabel.Text = $"Hello, {firstNameInput.Text}";
            top.Remove(addWin);
        };

        addWin.Add(firstNameLabel, firstNameInput, accept);
        top.Add(addWin);
    }
}

