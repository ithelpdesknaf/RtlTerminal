using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RtlTerminal;

public partial class FontSettingsWindow
{
    private TabItem _aiTab = null!;
    private ComboBox _aiProvider = null!;
    private TextBox _aiEndpoint = null!;
    private TextBox _aiModel = null!;
    private PasswordBox _aiApiKeyPassword = null!;
    private TextBox _aiApiKeyText = null!;
    private CheckBox _aiShowApiKey = null!;
    private CheckBox _aiAutoDiagnose = null!;
    private Button _aiTestConnectionButton = null!;
    private TextBlock _aiTestConnectionStatus = null!;

    public void ShowAiTab() => SettingsTabs.SelectedItem = _aiTab;

    private void InitializeAiSettings()
    {
        var config = AiConfig.Load();
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel = new StackPanel { Margin = new Thickness(20) };
        scroll.Content = panel;

        _aiTab = new TabItem { Header = "AI", Content = scroll };
        SettingsTabs.Items.Add(_aiTab);

        // Header
        panel.Children.Add(new TextBlock
        {
            Text = "إعدادات مساعد الذكاء الاصطناعي (AI Copilot)",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(24, 32, 51)),
            Margin = new Thickness(0, 0, 0, 4)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "اختر مزود الذكاء الاصطناعي، أدخل مفتاح API أو استخدم Ollama محلياً مجاناً.",
            Foreground = new SolidColorBrush(Color.FromRgb(88, 97, 116)),
            Margin = new Thickness(0, 0, 0, 16),
            TextWrapping = TextWrapping.Wrap
        });

        // Provider
        panel.Children.Add(new TextBlock { Text = "المزود (AI Provider)", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 4) });
        _aiProvider = new ComboBox
        {
            ItemsSource = new[] { "Ollama (محلي / Local)", "OpenAI (ChatGPT)", "Anthropic (Claude)", "Custom (OpenAI-Compatible)" },
            Margin = new Thickness(0, 0, 0, 12)
        };

        var initialIndex = config.Provider switch
        {
            "OpenAI" => 1,
            "Anthropic" => 2,
            "Custom" => 3,
            _ => 0
        };
        _aiProvider.SelectedIndex = initialIndex;
        _aiProvider.SelectionChanged += AiProvider_SelectionChanged;
        panel.Children.Add(_aiProvider);

        // Endpoint
        panel.Children.Add(new TextBlock { Text = "رابط الواجهة (API Endpoint URL)", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 4) });
        _aiEndpoint = new TextBox { Text = config.Endpoint, Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(6, 4, 6, 4) };
        panel.Children.Add(_aiEndpoint);

        // Model
        panel.Children.Add(new TextBlock { Text = "اسم النموذج (Model Name)", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 4) });
        _aiModel = new TextBox { Text = config.Model, Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(6, 4, 6, 4) };
        panel.Children.Add(_aiModel);

        // API Key
        panel.Children.Add(new TextBlock { Text = "مفتاح الوصول (API Key)", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 4) });
        var keyGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        _aiApiKeyPassword = new PasswordBox { Password = config.ApiKey, Padding = new Thickness(6, 4, 6, 4) };
        _aiApiKeyText = new TextBox { Text = config.ApiKey, Padding = new Thickness(6, 4, 6, 4), Visibility = Visibility.Collapsed };
        keyGrid.Children.Add(_aiApiKeyPassword);
        keyGrid.Children.Add(_aiApiKeyText);
        panel.Children.Add(keyGrid);

        _aiShowApiKey = new CheckBox { Content = "إظهار المفتاح (Show API Key)", Margin = new Thickness(0, 4, 0, 14) };
        _aiShowApiKey.Checked += (_, _) =>
        {
            _aiApiKeyText.Text = _aiApiKeyPassword.Password;
            _aiApiKeyPassword.Visibility = Visibility.Collapsed;
            _aiApiKeyText.Visibility = Visibility.Visible;
        };
        _aiShowApiKey.Unchecked += (_, _) =>
        {
            _aiApiKeyPassword.Password = _aiApiKeyText.Text;
            _aiApiKeyText.Visibility = Visibility.Collapsed;
            _aiApiKeyPassword.Visibility = Visibility.Visible;
        };
        panel.Children.Add(_aiShowApiKey);

        // Auto diagnose
        _aiAutoDiagnose = new CheckBox
        {
            Content = "تشخيص الأخطاء التلقائي عند الضغط على Ctrl+Shift+E",
            IsChecked = config.AutoDiagnoseErrors,
            Margin = new Thickness(0, 0, 0, 16)
        };
        panel.Children.Add(_aiAutoDiagnose);

        // Test Connection & Status
        var testBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 12) };
        _aiTestConnectionButton = new Button
        {
            Content = "اختبار الاتصال ⚡",
            Padding = new Thickness(12, 6, 12, 6),
            Cursor = System.Windows.Input.Cursors.Hand,
            Margin = new Thickness(0, 0, 12, 0)
        };
        _aiTestConnectionButton.Click += AiTestConnectionButton_Click;
        testBar.Children.Add(_aiTestConnectionButton);

        _aiTestConnectionStatus = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold
        };
        testBar.Children.Add(_aiTestConnectionStatus);
        panel.Children.Add(testBar);

        UpdateProviderUiState();
    }

    private void AiProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        switch (_aiProvider.SelectedIndex)
        {
            case 0: // Ollama
                _aiEndpoint.Text = "http://localhost:11434/api/generate";
                _aiModel.Text = "llama3";
                break;
            case 1: // OpenAI
                _aiEndpoint.Text = "https://api.openai.com/v1/chat/completions";
                _aiModel.Text = "gpt-4o-mini";
                break;
            case 2: // Anthropic
                _aiEndpoint.Text = "https://api.anthropic.com/v1/messages";
                _aiModel.Text = "claude-3-5-sonnet-20241022";
                break;
            case 3: // Custom
                if (string.IsNullOrEmpty(_aiEndpoint.Text) || _aiEndpoint.Text.Contains("localhost:11434", StringComparison.OrdinalIgnoreCase))
                    _aiEndpoint.Text = "https://api.openai.com/v1/chat/completions";
                break;
        }
        UpdateProviderUiState();
    }

    private void UpdateProviderUiState()
    {
        var isOllama = _aiProvider.SelectedIndex == 0;
        _aiApiKeyPassword.IsEnabled = !isOllama;
        _aiApiKeyText.IsEnabled = !isOllama;
        _aiShowApiKey.IsEnabled = !isOllama;
    }

    private async void AiTestConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        _aiTestConnectionButton.IsEnabled = false;
        _aiTestConnectionStatus.Foreground = Brushes.DimGray;
        _aiTestConnectionStatus.Text = "جاري اختبار الاتصال...";

        var apiKey = _aiShowApiKey.IsChecked == true ? _aiApiKeyText.Text : _aiApiKeyPassword.Password;
        var provider = _aiProvider.SelectedIndex switch
        {
            1 => "OpenAI",
            2 => "Anthropic",
            3 => "Custom",
            _ => "Ollama"
        };

        var config = new AiConfig
        {
            Provider = provider,
            Endpoint = _aiEndpoint.Text.Trim(),
            Model = _aiModel.Text.Trim(),
            ApiKey = apiKey.Trim()
        };

        var (success, message) = await AiService.TestConnectionAsync(config);
        _aiTestConnectionStatus.Foreground = success ? Brushes.ForestGreen : Brushes.Firebrick;
        _aiTestConnectionStatus.Text = message;
        _aiTestConnectionButton.IsEnabled = true;
    }

    private void SaveAiSettings()
    {
        var apiKey = _aiShowApiKey.IsChecked == true ? _aiApiKeyText.Text : _aiApiKeyPassword.Password;
        var provider = _aiProvider.SelectedIndex switch
        {
            1 => "OpenAI",
            2 => "Anthropic",
            3 => "Custom",
            _ => "Ollama"
        };

        var config = new AiConfig
        {
            Provider = provider,
            Endpoint = _aiEndpoint.Text.Trim(),
            Model = _aiModel.Text.Trim(),
            ApiKey = apiKey.Trim(),
            AutoDiagnoseErrors = _aiAutoDiagnose.IsChecked == true
        };
        config.Save();
    }
}
