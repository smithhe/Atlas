namespace Atlas.Api.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public int MaxPromptChars { get; set; } = 4_000;
    public int MaxContextChars { get; set; } = 12_000;
    public int MaxConcurrentSessions { get; set; } = 4;
    public string SystemPrompt { get; set; } =
        "You are Atlas AI assistant. Provide concise, practical guidance grounded only in supplied context. " +
        "If context is missing, clearly say what is missing.";
}

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4.1-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

