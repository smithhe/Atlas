using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Features.Ai.CreateConversation;

namespace Atlas.Tests.Unit.Features.Ai;

public sealed class CreateAiConversationCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToConversationService()
    {
        var service = new FakeAiConversationService();
        var handler = new CreateAiConversationCommandHandler(service);

        CreateAiConversationResult result = await handler.Handle(
            new CreateAiConversationCommand(
                Prompt: "Summarize risks",
                View: AiViewScope.Dashboard,
                ActionId: null,
                TaskId: null,
                ProjectId: null,
                RiskId: null,
                TeamMemberId: null),
            CancellationToken.None);

        Assert.Equal(service.LastConversationId, result.ConversationId);
        Assert.Equal(service.LastTurnSessionId, result.TurnSessionId);
        Assert.Equal("Summarize risks", service.LastPrompt);
    }

    private sealed class FakeAiConversationService : IAiConversationService
    {
        public Guid LastConversationId { get; private set; }

        public Guid LastTurnSessionId { get; private set; }

        public string? LastPrompt { get; private set; }

        public Task<(Guid ConversationId, Guid TurnSessionId)> StartConversationAsync(
            AiSessionStartRequest request,
            CancellationToken cancellationToken)
        {
            LastConversationId = request.ConversationId;
            LastTurnSessionId = Guid.NewGuid();
            LastPrompt = request.Prompt;
            return Task.FromResult((request.ConversationId, LastTurnSessionId));
        }

        public Task<Guid> ContinueConversationAsync(Guid conversationId, string prompt, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());
    }
}
