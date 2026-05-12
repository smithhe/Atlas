using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Features.Ai.CreateSession;
using Atlas.Api.DTOs.Ai;

namespace Atlas.Api.Endpoints.Ai;

public sealed class CreateAiSessionEndpoint : Endpoint<CreateAiSessionRequest, CreateAiSessionResponse>
{
    private readonly IMediator _mediator;

    public CreateAiSessionEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/ai/sessions");
        AllowAnonymous();
        Summary(s => { s.Summary = "Start a new AI session"; });
    }

    public override async Task HandleAsync(CreateAiSessionRequest req, CancellationToken ct)
    {
        Guid sessionId = await _mediator.Send(new CreateAiSessionCommand(
            Prompt: req.Prompt,
            View: req.View,
            ActionId: req.ActionId,
            TaskId: req.TaskId,
            ProjectId: req.ProjectId,
            RiskId: req.RiskId,
            TeamMemberId: req.TeamMemberId), ct);

        await Send.ResponseAsync(new CreateAiSessionResponse(sessionId), 202, ct);
    }
}

