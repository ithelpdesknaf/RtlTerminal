using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RtlTerminal;

internal static class AiHarnessTests
{
    public static void Run()
    {
        var window = new MainWindow(new DirectoryHistoryStore(Path.Combine(Path.GetTempPath(), "ai_history.json")));

        // 1. Structural checks in MainWindow
        var sidecar = window.FindName("AiSidecarDock") as FrameworkElement;
        if (sidecar is null) throw new Exception("AiSidecarDock element was not found in MainWindow");
        if (sidecar.Visibility != Visibility.Collapsed) throw new Exception("AiSidecarDock should be collapsed by default");

        var commandBar = window.FindName("AiCommandBar") as FrameworkElement;
        if (commandBar is null) throw new Exception("AiCommandBar element was not found in MainWindow");
        if (commandBar.Visibility != Visibility.Collapsed) throw new Exception("AiCommandBar should be collapsed by default");

        var toggleBtn = window.FindName("AiSidecarToggleButton") as Button;
        if (toggleBtn is null) throw new Exception("AiSidecarToggleButton element was not found in MainWindow");

        // 2. TerminalContextBundle verification
        var context = TerminalContextBundle.Capture(@"C:\MyProject", "Ubuntu", ["first output", "git checkout -b main", "done"]);
        if (!context.Contains("C:\\MyProject") || !context.Contains("Ubuntu") || !context.Contains("git checkout -b main"))
            throw new Exception("TerminalContextBundle failed to capture directory, distro, or output lines");

        // 3. AiConfig verification
        var config = new AiConfig { Provider = "Ollama", Model = "llama3" };
        if (config.Provider != "Ollama" || config.Model != "llama3")
            throw new Exception("AiConfig properties mismatch");

        // 4. AiService Heuristic Fallback verification
        var psResult = AiService.GenerateCommandAsync("ابحث عن أكبر الملفات", "PowerShell", @"C:\Test").GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(psResult) || (!psResult.Contains("Get-ChildItem") && !psResult.Contains("Sort-Object")))
            throw new Exception($"AiService heuristic failed for PowerShell: '{psResult}'");

        var bashResult = AiService.GenerateCommandAsync("فروع git", "WSL", @"C:\Test").GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(bashResult) || !bashResult.Contains("git branch"))
            throw new Exception($"AiService heuristic failed for bash: '{bashResult}'");

        Console.WriteLine("PASS AI harness sidecar dock, command bar, terminal context bundle and heuristic generation");
    }
}
