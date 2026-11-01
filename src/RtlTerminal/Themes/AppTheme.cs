using System.Windows;
using System.Windows.Media;

namespace RtlTerminal;

public enum AppThemeKind
{
    Dark,
    Light
}

public static class AppTheme
{
    private static readonly TerminalColor[] LightForegrounds =
    [
        new(36, 41, 47),
        new(207, 34, 46),
        new(17, 99, 41),
        new(125, 78, 0),
        new(5, 80, 174),
        new(130, 80, 223),
        new(27, 124, 131),
        new(87, 96, 106),
        new(110, 119, 129),
        new(164, 14, 38),
        new(26, 127, 55),
        new(99, 60, 1),
        new(9, 105, 218),
        new(130, 80, 223),
        new(49, 146, 170),
        new(36, 41, 47)
    ];

    // Applications pick colors assuming a dark screen, so explicit
    // backgrounds become light tints that keep the remapped text readable.
    private static readonly TerminalColor[] LightBackgrounds =
    [
        new(246, 248, 250),
        new(255, 235, 233),
        new(218, 251, 225),
        new(255, 248, 197),
        new(221, 244, 255),
        new(251, 239, 255),
        new(216, 243, 246),
        new(208, 215, 222),
        new(208, 215, 222),
        new(255, 206, 203),
        new(172, 238, 187),
        new(255, 236, 153),
        new(182, 227, 255),
        new(236, 215, 255),
        new(179, 233, 239),
        new(234, 238, 242)
    ];

    public static AppThemeKind Current { get; private set; } = AppThemeKind.Dark;

    // The selected color scheme, or null for the built-in colors of the theme.
    public static ColorScheme? Scheme { get; private set; }

    public static bool IsLight => Current == AppThemeKind.Light;

    public static TerminalColor DefaultForeground =>
        Scheme?.DefaultForeground ?? (IsLight ? new(36, 41, 47) : new(230, 230, 230));

    public static TerminalColor DefaultBackground =>
        Scheme?.DefaultBackground ?? (IsLight ? new(255, 255, 255) : new(12, 12, 12));

    public static TerminalColor MapForeground(TerminalColor color, int? paletteIndex = null) =>
        paletteIndex is not (>= 0 and < 16) ? color
        : Scheme is not null ? Scheme.Text(paletteIndex.Value)
        : IsLight ? LightForegrounds[paletteIndex.Value]
        : color;

    public static TerminalColor MapBackground(TerminalColor color, int? paletteIndex = null) =>
        paletteIndex is not (>= 0 and < 16) ? color
        : Scheme is not null ? Scheme.Fill(paletteIndex.Value)
        : IsLight ? LightBackgrounds[paletteIndex.Value]
        : color;

    internal static TerminalColor DefaultAnsiForeground(AppThemeKind theme, int index) =>
        theme == AppThemeKind.Light ? LightForegrounds[index] : ColorScheme.Find("Campbell")!.Text(index);

    public static Color TabAccent => IsLight ? Rgb(13, 148, 136) : Rgb(45, 212, 191);
    public static Color TabIcon => IsLight ? Rgb(101, 109, 118) : Rgb(156, 163, 175);
    public static Color ActiveTabText => IsLight ? Rgb(31, 35, 40) : Rgb(248, 250, 252);
    public static Color InactiveTabText => IsLight ? Rgb(87, 96, 106) : Rgb(156, 163, 175);
    public static Color ActiveTab => IsLight ? Rgb(246, 248, 250) : Rgb(30, 31, 36);
    public static Color HoverTab => IsLight ? Rgb(215, 220, 226) : Rgb(38, 40, 48);
    public static Color TabSeparator => IsLight ? Rgb(175, 184, 193) : Rgb(48, 50, 60);
    public static Color CursorFill => IsLight ? Color.FromArgb(90, 36, 41, 47) : Color.FromArgb(90, 230, 230, 230);
    public static Color ScrollTrack => IsLight ? Color.FromArgb(10, 0, 0, 0) : Color.FromArgb(14, 255, 255, 255);
    public static Color ScrollThumb => IsLight ? Color.FromArgb(105, 0, 0, 0) : Color.FromArgb(100, 255, 255, 255);
    public static Color ScrollThumbHover => IsLight ? Color.FromArgb(150, 0, 0, 0) : Color.FromArgb(160, 255, 255, 255);
    public static Color ScrollThumbPressed => IsLight ? Color.FromArgb(185, 0, 0, 0) : Color.FromArgb(200, 255, 255, 255);
    public static Color CursorBorder => IsLight ? Rgb(110, 119, 129) : Colors.LightGray;

    // A scheme switches to its own dark or light theme.
    public static void Apply(AppThemeKind theme, ColorScheme? scheme = null)
    {
        Current = scheme?.Theme ?? theme;
        Scheme = scheme;
        theme = Current;
        TerminalBuffer.DefaultForeground = DefaultForeground;
        TerminalBuffer.DefaultBackground = DefaultBackground;
        var light = theme == AppThemeKind.Light;
        var resources = Application.Current.Resources;

        void Set(string key, byte r, byte g, byte b, byte lr, byte lg, byte lb)
        {
            resources[key] = Frozen(light ? Rgb(lr, lg, lb) : Rgb(r, g, b));
        }

        Set("TerminalBackgroundBrush", 12, 12, 12, 255, 255, 255);
        Set("TerminalForegroundBrush", 230, 230, 230, 36, 41, 47);
        Set("TitleBarBrush", 24, 24, 28, 222, 226, 231);
        Set("ToolbarBrush", 30, 31, 36, 246, 248, 250);
        Set("TabRenameBackgroundBrush", 20, 20, 24, 255, 255, 255);
        Set("DirectoryBadgeBrush", 36, 38, 46, 233, 236, 240);
        Set("DirectoryBadgeHoverBrush", 46, 48, 58, 225, 229, 234);
        Set("DirectoryBadgeForegroundBrush", 103, 232, 249, 14, 116, 144);
        Set("ToolbarBorderBrush", 44, 46, 54, 208, 215, 222);
        Set("ChromeForegroundBrush", 230, 232, 238, 31, 35, 40);
        Set("ChromeStrongForegroundBrush", 255, 255, 255, 0, 0, 0);
        Set("MutedForegroundBrush", 156, 163, 175, 87, 96, 106);
        Set("AccentBrush", 45, 212, 191, 13, 148, 136);
        Set("FocusBrush", 56, 189, 248, 11, 87, 208);
        Set("MenuBackgroundBrush", 28, 29, 34, 255, 255, 255);
        Set("MenuBorderBrush", 48, 50, 60, 208, 215, 222);
        Set("MenuHighlightBrush", 42, 44, 54, 234, 238, 242);
        Set("MenuOpenBrush", 48, 50, 62, 220, 225, 230);
        Set("SeparatorBrush", 46, 48, 58, 216, 222, 228);
        Set("CaptionHoverBrush", 44, 46, 56, 205, 210, 216);
        Set("CaptionPressedBrush", 56, 58, 70, 190, 196, 203);
        Set("ButtonPressedBrush", 50, 52, 64, 208, 215, 222);
        Set("RoundButtonHoverBrush", 44, 46, 56, 205, 210, 216);
        Set("RoundButtonPressedBrush", 56, 58, 70, 190, 196, 203);
        Set("RoundButtonFocusBrush", 45, 212, 191, 196, 214, 240);
        Set("WindowBorderBrush", 48, 50, 58, 208, 215, 222);

        if (scheme is not null)
        {
            resources["TerminalBackgroundBrush"] = Frozen(ToColor(scheme.DefaultBackground));
            resources["TerminalForegroundBrush"] = Frozen(ToColor(scheme.DefaultForeground));
        }

        // Menus in the window share the terminal scrollbar style.
        resources["ScrollTrackBrush"] = Frozen(ScrollTrack);
        resources["ScrollThumbBrush"] = Frozen(ScrollThumb);
        resources["ScrollThumbHoverBrush"] = Frozen(ScrollThumbHover);
        resources["ScrollThumbPressedBrush"] = Frozen(ScrollThumbPressed);
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color ToColor(TerminalColor color) =>
        Color.FromRgb(color.Red, color.Green, color.Blue);

    private static Color Rgb(byte red, byte green, byte blue) =>
        Color.FromRgb(red, green, blue);
}
