using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace RtlTerminal;

/// <summary>
/// Captures and packages terminal environment context (working directory, git branch, and recent output).
/// </summary>
public static class TerminalContextBundle
{
    public static string Capture(string? directory, string? wslDistro, string[] recentLines, int lineCount = 40)
    {
        var sb = new StringBuilder();
        sb.AppendLine("### Terminal Context");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **OS**: Windows {Environment.OSVersion.Version} {(string.IsNullOrEmpty(wslDistro) ? "" : $"(WSL: {wslDistro})")}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **Directory**: `{directory ?? "Unknown"}`");

        var gitInfo = TryGetGitBranch(directory);
        if (!string.IsNullOrEmpty(gitInfo))
            sb.AppendLine(CultureInfo.InvariantCulture, $"- **Git Branch**: `{gitInfo}`");

        sb.AppendLine();
        sb.AppendLine("### Recent Terminal Output");
        sb.AppendLine("```shell");

        var take = Math.Min(recentLines.Length, lineCount);
        var start = Math.Max(0, recentLines.Length - take);
        for (var i = start; i < recentLines.Length; i++)
        {
            var line = recentLines[i];
            if (!string.IsNullOrWhiteSpace(line))
                sb.AppendLine(line);
        }

        sb.AppendLine("```");
        return sb.ToString();
    }

    private static string? TryGetGitBranch(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --abbrev-ref HEAD",
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc is null) return null;
            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(1000);
            return proc.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
