using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RtlTerminal;

/// <summary>
/// Configures modern Windows 11 Desktop Window Manager (DWM) visual attributes
/// including rounded corners, immersive dark mode, and subtle borders.
/// </summary>
internal static class ModernWindowChrome
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window, bool isDark)
    {
        if (Environment.OSVersion.Version.Major < 10) return;

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;

        // 1. Enable Windows 11 rounded window corners (build 22000+)
        var roundCorners = DWMWCP_ROUND;
        _ = DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref roundCorners, sizeof(int));

        // 2. Set immersive dark/light mode for title chrome
        var darkMode = isDark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        // 3. Subtle custom window border color on Windows 11 (0x00BBGGRR format)
        var borderColor = isDark ? 0x003A3230 : 0x00D8D0C8;
        _ = DwmSetWindowAttribute(handle, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
    }
}
