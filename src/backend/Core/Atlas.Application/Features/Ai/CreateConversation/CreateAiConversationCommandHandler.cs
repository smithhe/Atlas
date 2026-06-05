using Atlas.Application.Abstractions.Ai;

namespace Atlas.Application.Features.Ai.CreateConversation;

public sealed class CreateAiConversationCommandHandler : IRequestHandler<CreateAiConversationCommand, CreateAiConversationResult>
{
    private readonly IAiConversationService _conversationService;

    public CreateAiConversationCommandHandler(IAiConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    public async Task<CreateAiConversationResult> Handle(CreateAiConversationCommand request, CancellationToken cancellationToken)
    {
        var conversationId = Guid.NewGuid();
        var startRequest = new AiSessionStartRequest(
            ConversationId: conversationId,
            TurnIndex: 0,
            Prompt: request.Prompt,
            View: request.View,
            ActionId: request.ActionId,
            TaskId: request.TaskId,
            ProjectId: request.ProjectId,
            RiskId: request.RiskId,
            TeamMemberId: request.TeamMemberId);

        (Guid _, Guid turnSessionId) = await _conversationService.StartConversationAsync(startRequest, cancellationToken);
        return new CreateAiConversationResult(conversationId, turnSessionId);
    }
}
