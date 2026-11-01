using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RtlTerminal;

/// <summary>
/// Core AI service delivering CLI command generation, error diagnosis, and sidecar chat.
/// </summary>
public static class AiService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(25) };

    public static async Task<string> GenerateCommandAsync(string naturalLanguagePrompt, string shellType, string? directory)
    {
        var config = AiConfig.Load();
        var systemPrompt = $"""
            You are a terminal CLI assistant. The user wants a command for shell '{shellType}' in directory '{directory ?? "unknown"}'.
            The prompt may be in Arabic or English.
            Respond ONLY with the single exact command or script to execute, without markdown formatting, code fences, or explanations.
            """;

        try
        {
            var response = await CallLlmAsync(config, systemPrompt, naturalLanguagePrompt);
            return CleanCommandResponse(response);
        }
        catch
        {
            return FallbackCommandHeuristic(naturalLanguagePrompt, shellType);
        }
    }

    public static async Task<(string Explanation, string? FixCommand)> DiagnoseErrorAsync(string errorSnippet, string? lastCommand, string shellType)
    {
        var config = AiConfig.Load();
        var systemPrompt = $"""
            You are a terminal error diagnostic expert.
            The user encountered an error in shell '{shellType}' when running: '{lastCommand ?? "unknown"}'.
            Diagnose the error and explain the cause in Arabic briefly (1-2 sentences).
            Then provide a suggested fix command if applicable.
            Format your response exactly as:
            CAUSE: [Brief Arabic explanation]
            FIX: [Exact CLI command or NONE]
            """;

        try
        {
            var raw = await CallLlmAsync(config, systemPrompt, errorSnippet);
            var lines = raw.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var cause = "";
            string? fix = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("CAUSE:", StringComparison.OrdinalIgnoreCase))
                    cause = line[6..].Trim();
                else if (line.StartsWith("FIX:", StringComparison.OrdinalIgnoreCase))
                {
                    var cmd = line[4..].Trim();
                    if (!string.Equals(cmd, "NONE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(cmd))
                        fix = CleanCommandResponse(cmd);
                }
            }

            if (string.IsNullOrEmpty(cause)) cause = raw;
            return (cause, fix);
        }
        catch
        {
            return ("تعذر الاتصال بمزود الذكاء الاصطناعي (تحقق من إعدادات Ollama أو API Key).", null);
        }
    }

    public static async Task<string> AskSidecarAsync(string userMessage, string context)
    {
        var config = AiConfig.Load();
        var systemPrompt = """
            You are an expert AI companion embedded in RtlTerminal (a professional RTL terminal with Arabic/Persian/Hebrew support).
            Provide concise, accurate terminal and programming assistance.
            Respond in the user's language (Arabic or English).
            Wrap CLI commands in ```shell code blocks.
            """;

        var fullPrompt = $"{context}\n\nUser Question: {userMessage}";
        try
        {
            return await CallLlmAsync(config, systemPrompt, fullPrompt);
        }
        catch (Exception ex)
        {
            return $"خطأ في الاتصال بالذكاء الاصطناعي: {ex.Message}\nتأكد من تشغيل Ollama محلياً على المنفذ 11434 أو إدخال API Key في الإعدادات.";
        }
    }

    public static async Task<(bool Success, string Message)> TestConnectionAsync(AiConfig config)
    {
        try
        {
            var prompt = "Ping test. Please reply with: OK";
            var reply = await CallLlmAsync(config, "You are an API connection verifier. Respond strictly with 'OK'.", prompt);
            if (!string.IsNullOrWhiteSpace(reply))
                return (true, $"✓ تم الاتصال بنجاح بالنموذج {config.Model}!");
            return (false, "لم يتم استلام رد من المزود.");
        }
        catch (Exception ex)
        {
            return (false, $"فشل الاتصال: {ex.Message}");
        }
    }

    private static async Task<string> CallLlmAsync(AiConfig config, string systemPrompt, string prompt)
    {
        if (config.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            var body = new JsonObject
            {
                ["model"] = string.IsNullOrEmpty(config.Model) ? "llama3" : config.Model,
                ["system"] = systemPrompt,
                ["prompt"] = prompt,
                ["stream"] = false
            };
            var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
            var res = await HttpClient.PostAsync(config.Endpoint, content);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonNode.Parse(json);
            return doc?["response"]?.ToString() ?? "";
        }
        else if (config.Provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            var body = new JsonObject
            {
                ["model"] = string.IsNullOrEmpty(config.Model) ? "claude-3-5-sonnet-20241022" : config.Model,
                ["system"] = systemPrompt,
                ["max_tokens"] = 1000,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "user", ["content"] = prompt }
                }
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.Add("x-api-key", config.ApiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
            var res = await HttpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonNode.Parse(json);
            return doc?["content"]?[0]?["text"]?.ToString() ?? "";
        }
        else
        {
            // OpenAI or compatible endpoint
            var url = string.IsNullOrEmpty(config.Endpoint) || config.Endpoint.Contains("localhost:11434")
                ? "https://api.openai.com/v1/chat/completions"
                : config.Endpoint;
            var body = new JsonObject
            {
                ["model"] = string.IsNullOrEmpty(config.Model) ? "gpt-4o-mini" : config.Model,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
                    new JsonObject { ["role"] = "user", ["content"] = prompt }
                }
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrEmpty(config.ApiKey))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
            var res = await HttpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonNode.Parse(json);
            return doc?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
        }
    }

    private static string CleanCommandResponse(string raw)
    {
        var cleaned = raw.Trim();
        if (cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = cleaned.IndexOf('\n');
            var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNl > 0 && lastFence > firstNl)
                cleaned = cleaned[(firstNl + 1)..lastFence].Trim();
        }
        return cleaned.Trim('`', ' ', '\r', '\n');
    }

    private static string FallbackCommandHeuristic(string prompt, string shellType)
    {
        var lower = prompt.ToLowerInvariant();
        var isPowerShell = shellType.Contains("PowerShell", StringComparison.OrdinalIgnoreCase);

        if (lower.Contains("حجم") || lower.Contains("أكبر") || lower.Contains("كبير") || lower.Contains("large") || lower.Contains("size"))
            return isPowerShell
                ? "Get-ChildItem -File -Recurse | Sort-Object Length -Descending | Select-Object -First 10 Name, Length"
                : "find . -type f -exec ls -lh {} + | sort -k 5 -hr | head -n 10";

        if (lower.Contains("git") || lower.Contains("فروع") || lower.Contains("branch"))
            return "git branch -a";

        if (lower.Contains("تحديث") || lower.Contains("update"))
            return isPowerShell ? "winget upgrade" : "sudo apt update && sudo apt upgrade -y";

        if (lower.Contains("ip") || lower.Contains("شبكة") || lower.Contains("network"))
            return isPowerShell ? "Get-NetIPAddress | Format-Table" : "ip a";

        return isPowerShell ? "Get-ChildItem" : "ls -la";
    }
}
