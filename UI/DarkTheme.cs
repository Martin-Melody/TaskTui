// UI/DarkTheme.cs
using Terminal.Gui;

namespace TaskTui.UI;

public static class DarkTheme
{
    // Base dark scheme for windows/content
    public static readonly ColorScheme Base = Make(Color.White, Color.Black, Color.Black, Color.Black, Color.BrightCyan);

    // Menus / status bar
    public static readonly ColorScheme Menu = Make(Color.Black, Color.Gray, Color.Black, Color.Black, Color.BrightCyan);

    // Dialogs
    public static readonly ColorScheme Dialog = Make(Color.White, Color.Black, Color.Black, Color.Black, Color.BrightCyan);

    // Tables (selected row = cyan background)
    public static readonly ColorScheme Table = new ColorScheme
    {
        // normal rows
        Normal = Application.Driver.MakeAttribute(Color.White, Color.Black),

        // selected row WHEN the table has focus -> bright text, same black bg
        Focus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black),

        // selected row when the table does NOT have focus (e.g., menu focused)
        HotNormal = Application.Driver.MakeAttribute(Color.Gray, Color.Black),

        // “hot” while focused (rarely used by TableView, but keep consistent)
        HotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black),

        Disabled = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
    };
    // Errors
    public static readonly ColorScheme Error = Make(Color.White, Color.Red, Color.BrightYellow, Color.White, Color.Red);

    public static void ApplyToApp()
    {
        // apply globally (do this AFTER Application.Init and BEFORE building views)
        Colors.Base = Base;
        Colors.TopLevel = Base;
        Colors.Menu = Menu;
        Colors.Dialog = Dialog;
        Colors.Error = Error;
    }

    private static ColorScheme Make(Color fg, Color bg, Color hot, Color focusFg, Color focusBg) =>
        new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(fg, bg),
            HotNormal = Application.Driver.MakeAttribute(hot, bg),
            Focus = Application.Driver.MakeAttribute(focusFg, focusBg),
            HotFocus = Application.Driver.MakeAttribute(hot, focusBg),
            Disabled = Application.Driver.MakeAttribute(Color.DarkGray, bg),
        };
}

