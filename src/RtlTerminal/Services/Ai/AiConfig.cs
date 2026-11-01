using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RtlTerminal;

/// <summary>
/// Configuration for AI providers (Local Ollama, OpenAI, Anthropic, or Custom endpoint).
/// </summary>
public sealed class AiConfig
{
    public string Provider { get; set; } = "Ollama"; // "Ollama", "OpenAI", "Anthropic", "Custom"
    public string Endpoint { get; set; } = "http://localhost:11434/api/generate";
    public string Model { get; set; } = "llama3";
    public string ApiKey { get; set; } = "";
    public bool AutoDiagnoseErrors { get; set; } = true;

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RtlTerminal",
        "ai_config.json");

    public static AiConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AiConfig>(json) ?? new AiConfig();
            }
        }
        catch { }
        return new AiConfig();
    }

    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, IndentedJson);
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}
