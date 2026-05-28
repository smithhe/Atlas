namespace Atlas.Application.Abstractions.Ai;

public sealed record AiModelRequest(
    string SystemPrompt,
    IReadOnlyList<AiChatMessage> Messages);

