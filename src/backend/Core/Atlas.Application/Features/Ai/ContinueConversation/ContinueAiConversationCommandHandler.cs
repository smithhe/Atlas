using Atlas.Application.Abstractions.Ai;

namespace Atlas.Application.Features.Ai.ContinueConversation;

public sealed class ContinueAiConversationCommandHandler : IRequestHandler<ContinueAiConversationCommand, ContinueAiConversationResult>
{
    private readonly IAiConversationService _conversationService;

    public ContinueAiConversationCommandHandler(IAiConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    public async Task<ContinueAiConversationResult> Handle(ContinueAiConversationCommand request, CancellationToken cancellationToken)
    {
        Guid turnSessionId = await _conversationService.ContinueConversationAsync(
            request.ConversationId,
            request.Prompt,
            cancellationToken);

        return new ContinueAiConversationResult(turnSessionId);
    }
}
