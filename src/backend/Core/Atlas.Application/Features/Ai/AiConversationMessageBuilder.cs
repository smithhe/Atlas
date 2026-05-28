using Atlas.Application.Abstractions.Ai;

namespace Atlas.Application.Features.Ai;

public static class AiConversationMessageBuilder
{
    public static IReadOnlyList<AiChatMessage> BuildTurnZeroMessages(string systemPrompt, string composedUserPrompt)
    {
        return
        [
            new AiChatMessage("system", systemPrompt),
            new AiChatMessage("user", composedUserPrompt),
        ];
    }

    public static IReadOnlyList<AiChatMessage> BuildFollowUpMessages(
        string systemPrompt,
        IReadOnlyList<AiConversationTurnHistory> priorTurns,
        string userPrompt,
        int maxHistoryChars)
    {
        var messages = new List<AiChatMessage> { new("system", systemPrompt) };
        int usedChars = systemPrompt.Length;

        foreach (AiConversationTurnHistory turn in priorTurns)
        {
            int turnChars = turn.Prompt.Length + turn.AssistantResponse.Length;
            if (usedChars + turnChars > maxHistoryChars)
            {
                break;
            }

            messages.Add(new AiChatMessage("user", turn.Prompt));
            messages.Add(new AiChatMessage("assistant", turn.AssistantResponse));
            usedChars += turnChars;
        }

        messages.Add(new AiChatMessage("user", userPrompt));
        return messages;
    }

    public static string BuildComposedUserPrompt(AiSessionStartRequest request, string context, string userPrompt)
    {
        return
            $"Atlas view: {request.View}\n" +
            $"Action: {(string.IsNullOrWhiteSpace(request.ActionId) ? "none" : request.ActionId)}\n\n" +
            "Context:\n" +
            $"{context}\n\n" +
            "User request:\n" +
            $"{userPrompt}";
    }
}

public sealed record AiConversationTurnHistory(string Prompt, string AssistantResponse);
