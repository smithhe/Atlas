using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Features.Ai;

namespace Atlas.Application.Tests.Features.Ai;

public sealed class AiConversationMessageBuilderTests
{
    [Fact]
    public void BuildTurnZeroMessages_includes_system_and_composed_user_prompt()
    {
        const string system = "You are Atlas.";
        const string user = "Atlas view: Dashboard\nUser request:\nHello";

        IReadOnlyList<AiChatMessage> messages = AiConversationMessageBuilder.BuildTurnZeroMessages(system, user);

        Assert.Equal(2, messages.Count);
        Assert.Equal("system", messages[0].Role);
        Assert.Equal(system, messages[0].Content);
        Assert.Equal("user", messages[1].Role);
        Assert.Equal(user, messages[1].Content);
    }

    [Fact]
    public void BuildFollowUpMessages_includes_prior_turns_and_new_user_prompt()
    {
        IReadOnlyList<AiConversationTurnHistory> history =
        [
            new("What are my top risks?", "Risk A and Risk B are highest."),
        ];

        IReadOnlyList<AiChatMessage> messages = AiConversationMessageBuilder.BuildFollowUpMessages(
            "You are Atlas.",
            history,
            "Which should I tackle first?",
            maxHistoryChars: 8000);

        Assert.Equal(4, messages.Count);
        Assert.Equal("system", messages[0].Role);
        Assert.Equal("user", messages[1].Role);
        Assert.Equal("What are my top risks?", messages[1].Content);
        Assert.Equal("assistant", messages[2].Role);
        Assert.Equal("Risk A and Risk B are highest.", messages[2].Content);
        Assert.Equal("user", messages[3].Role);
        Assert.Equal("Which should I tackle first?", messages[3].Content);
    }

    [Fact]
    public void BuildFollowUpMessages_truncates_oldest_turns_when_history_exceeds_limit()
    {
        IReadOnlyList<AiConversationTurnHistory> history =
        [
            new("First question with a long prompt.", "First answer."),
            new("Second question.", "Second answer."),
        ];

        IReadOnlyList<AiChatMessage> messages = AiConversationMessageBuilder.BuildFollowUpMessages(
            "sys",
            history,
            "Third",
            maxHistoryChars: 50);

        Assert.DoesNotContain(messages, m => m.Content.Contains("Second question"));
        Assert.Contains(messages, m => m.Content.Contains("First question"));
        Assert.Contains(messages, m => m.Content == "Third");
    }

    [Fact]
    public void BuildComposedUserPrompt_includes_view_context_and_user_request()
    {
        var request = new AiSessionStartRequest(
            ConversationId: Guid.NewGuid(),
            TurnIndex: 0,
            Prompt: "Summarize",
            View: AiViewScope.Dashboard,
            ActionId: "brief",
            TaskId: null,
            ProjectId: null,
            RiskId: null,
            TeamMemberId: null);

        string composed = AiConversationMessageBuilder.BuildComposedUserPrompt(request, "ctx-data", "Summarize");

        Assert.Contains("Atlas view: Dashboard", composed);
        Assert.Contains("Action: brief", composed);
        Assert.Contains("ctx-data", composed);
        Assert.Contains("Summarize", composed);
    }

    [Fact]
    public void Two_turn_conversation_second_call_uses_history_not_context_block()
    {
        IReadOnlyList<AiConversationTurnHistory> afterFirstTurn =
        [
            new("List urgent tasks", "You have 3 urgent tasks."),
        ];

        IReadOnlyList<AiChatMessage> secondTurnMessages = AiConversationMessageBuilder.BuildFollowUpMessages(
            "Atlas assistant",
            afterFirstTurn,
            "Tell me more about the first one",
            maxHistoryChars: 16000);

        Assert.All(secondTurnMessages, m => Assert.DoesNotContain("Context:", m.Content));
        Assert.Equal("Tell me more about the first one", secondTurnMessages[^1].Content);
    }
}
