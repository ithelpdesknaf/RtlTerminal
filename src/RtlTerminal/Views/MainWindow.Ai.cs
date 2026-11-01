using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RtlTerminal;

public partial class MainWindow
{
    private static void InitializeAiHarness()
    {
        // Ready for AI interactions
    }

    private static string[] ExtractTerminalLines(TerminalSnapshot? snapshot)
    {
        if (snapshot is null) return [];
        var result = new List<string>(snapshot.Lines.Count);
        foreach (var line in snapshot.Lines)
        {
            var sb = new StringBuilder();
            foreach (var run in line.Runs)
                sb.Append(run.Text);
            var text = sb.ToString().TrimEnd();
            if (!string.IsNullOrWhiteSpace(text))
                result.Add(text);
        }
        return result.ToArray();
    }

    private void AiCommandBarMenuItem_Click(object sender, RoutedEventArgs e) => OpenAiCommandBar();

    private void ToggleAiSidecar()
    {
        if (AiSidecarDock.Visibility == Visibility.Visible)
        {
            AiSidecarDock.Visibility = Visibility.Collapsed;
            ActiveView.Focus();
        }
        else
        {
            AiSidecarDock.Visibility = Visibility.Visible;
            if (AiChatMessagesHost.Children.Count == 0)
            {
                AddSidecarAiCard(
                    "مرحباً بك في مساعد الطرفية الذكي!\n\n" +
                    "• اضغط `Ctrl+I` لتوليد الأوامر باللغة الطبيعية.\n" +
                    "• اضغط `Ctrl+Shift+E` لتشخيص الأخطاء فوراً.\n" +
                    "• اضغط `Ctrl+Shift+A` لنسخ سياق الطرفية كاملاً.\n\n" +
                    "كيف يمكنني مساعدتك الآن؟");
            }
            AiSidecarInputBox.Focus();
        }
    }

    private void OpenAiCommandBar()
    {
        AiCommandBar.Visibility = Visibility.Visible;
        AiCommandTextBox.Text = "";
        AiCommandPlaceholder.Visibility = Visibility.Visible;
        AiCommandPreviewRow.Visibility = Visibility.Collapsed;
        AiCommandTextBox.Focus();
    }

    private void CloseAiCommandBar()
    {
        AiCommandBar.Visibility = Visibility.Collapsed;
        ActiveView.Focus();
    }

    private void AiSidecarToggleButton_Click(object sender, RoutedEventArgs e) => ToggleAiSidecar();
    private void AiCloseSidecarButton_Click(object sender, RoutedEventArgs e) => ToggleAiSidecar();
    private void AiCommandCloseButton_Click(object sender, RoutedEventArgs e) => CloseAiCommandBar();

    private void AiCommandTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        AiCommandPlaceholder.Visibility = string.IsNullOrEmpty(AiCommandTextBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            GenerateAiCommand();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CloseAiCommandBar();
        }
    }

    private async void AiCommandGenerateButton_Click(object sender, RoutedEventArgs e) => GenerateAiCommand();

    private async void GenerateAiCommand()
    {
        var prompt = AiCommandTextBox.Text.Trim();
        if (string.IsNullOrEmpty(prompt)) return;

        AiGeneratedCommandText.Text = "جاري توليد الأمر بالذكاء الاصطناعي...";
        AiCommandPreviewRow.Visibility = Visibility.Visible;
        AiRunCommandButton.IsEnabled = false;

        var pane = ExplorerTargetPane();
        var shellType = pane?.Profile.ToString() ?? "PowerShell";
        var dir = pane?.CurrentDirectory;

        var command = await AiService.GenerateCommandAsync(prompt, shellType, dir);
        AiGeneratedCommandText.Text = command;
        AiRunCommandButton.IsEnabled = !string.IsNullOrWhiteSpace(command);
    }

    private void AiRunCommandButton_Click(object sender, RoutedEventArgs e)
    {
        var cmd = AiGeneratedCommandText.Text.Trim();
        if (!string.IsNullOrEmpty(cmd))
        {
            ExecuteInActivePane(cmd);
        }
        CloseAiCommandBar();
    }

    private void ExecuteInActivePane(string command)
    {
        var pane = ExplorerTargetPane();
        if (pane?.Session is { } session)
        {
            session.Write(command + "\r");
        }
        ActiveView.Focus();
    }

    private async void AiDiagnoseErrorButton_Click(object sender, RoutedEventArgs e) => DiagnoseLastError();

    public async void DiagnoseLastError()
    {
        if (AiSidecarDock.Visibility != Visibility.Visible)
            ToggleAiSidecar();

        var pane = ExplorerTargetPane();
        var lines = ExtractTerminalLines(pane?.Buffer?.CaptureSnapshot());
        var errorSnippet = string.Join("\n", lines.TakeLast(15));
        var shellType = pane?.Profile.ToString() ?? "PowerShell";

        AddSidecarUserCard("🔍 فحص وتشخيص الخطأ الأخير...");
        var (cause, fix) = await AiService.DiagnoseErrorAsync(errorSnippet, null, shellType);

        var response = $"**تشخيص الخطأ:**\n{cause}";
        if (!string.IsNullOrEmpty(fix))
        {
            response += $"\n\n**الأمر المقترح للإصلاح:**\n```{fix}```";
        }
        AddSidecarAiCard(response, fix);
    }

    public void CopyTerminalContext()
    {
        var pane = ExplorerTargetPane();
        var lines = ExtractTerminalLines(pane?.Buffer?.CaptureSnapshot());
        var bundle = TerminalContextBundle.Capture(pane?.CurrentDirectory, pane?.WslDistribution, lines);

        Clipboard.SetText(bundle);
        if (AiSidecarDock.Visibility == Visibility.Visible)
        {
            AddSidecarUserCard("📋 تم نسخ سياق الطرفية كاملاً إلى الحافظة (Terminal Context).");
        }
    }

    private void AiCopyContextButton_Click(object sender, RoutedEventArgs e) => CopyTerminalContext();

    private void AiClearChatButton_Click(object sender, RoutedEventArgs e) => AiChatMessagesHost.Children.Clear();

    private void AiSidecarInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            SendSidecarMessage();
        }
    }

    private void AiSidecarSendButton_Click(object sender, RoutedEventArgs e) => SendSidecarMessage();

    private async void SendSidecarMessage()
    {
        var text = AiSidecarInputBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        AiSidecarInputBox.Text = "";
        AddSidecarUserCard(text);

        var pane = ExplorerTargetPane();
        var lines = ExtractTerminalLines(pane?.Buffer?.CaptureSnapshot());
        var context = TerminalContextBundle.Capture(pane?.CurrentDirectory, pane?.WslDistribution, lines, 20);

        var reply = await AiService.AskSidecarAsync(text, context);
        AddSidecarAiCard(reply);
    }

    private void AddSidecarUserCard(string message)
    {
        var border = new Border
        {
            Background = (Brush)FindResource("DirectoryBadgeBrush"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(20, 4, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        border.Child = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("ChromeStrongForegroundBrush"),
            FontSize = 12
        };
        AiChatMessagesHost.Children.Add(border);
        AiChatScroller.ScrollToEnd();
    }

    private void AddSidecarAiCard(string message, string? runnableCommand = null)
    {
        var border = new Border
        {
            Background = (Brush)FindResource("TerminalBackgroundBrush"),
            BorderBrush = (Brush)FindResource("ToolbarBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 4, 20, 8),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("ChromeForegroundBrush"),
            FontSize = 12,
            LineHeight = 18
        });

        if (!string.IsNullOrEmpty(runnableCommand))
        {
            var runBtn = new Button
            {
                Content = $"تنفيذ: {runnableCommand} ▷",
                Margin = new Thickness(0, 8, 0, 0),
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = (Brush)FindResource("AccentBrush"),
                Foreground = (Brush)FindResource("TerminalBackgroundBrush"),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Cursor = Cursors.Hand
            };
            runBtn.Click += (_, _) => ExecuteInActivePane(runnableCommand);
            stack.Children.Add(runBtn);
        }

        border.Child = stack;
        AiChatMessagesHost.Children.Add(border);
        AiChatScroller.ScrollToEnd();
    }
}
