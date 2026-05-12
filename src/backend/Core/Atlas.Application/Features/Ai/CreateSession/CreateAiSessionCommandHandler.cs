using Atlas.Application.Abstractions.Ai;

namespace Atlas.Application.Features.Ai.CreateSession;

public sealed class CreateAiSessionCommandHandler : IRequestHandler<CreateAiSessionCommand, Guid>
{
    private readonly IAiSessionService _sessionService;

    public CreateAiSessionCommandHandler(IAiSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public Task<Guid> Handle(CreateAiSessionCommand request, CancellationToken cancellationToken)
    {
        var startRequest = new AiSessionStartRequest(
            Prompt: request.Prompt,
            View: request.View,
            ActionId: request.ActionId,
            TaskId: request.TaskId,
            ProjectId: request.ProjectId,
            RiskId: request.RiskId,
            TeamMemberId: request.TeamMemberId);

        return _sessionService.StartSessionAsync(startRequest, cancellationToken);
    }
}

