using Atlas.Api.DTOs.Ai;
using Atlas.Application.Features.Ai.CreateConversation;

namespace Atlas.Api.Endpoints.Ai;

public sealed class CreateAiConversationEndpoint : Endpoint<CreateAiConversationRequest, CreateAiConversationResponse>
{
    private readonly IMediator _mediator;

    public CreateAiConversationEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/ai/conversations");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Start a new AI conversation";
            s.Response<CreateAiConversationResponse>(202, "Accepted");
        });
    }

    public override async Task HandleAsync(CreateAiConversationRequest req, CancellationToken ct)
    {
        CreateAiConversationResult result = await _mediator.Send(new CreateAiConversationCommand(
            Prompt: req.Prompt,
            View: req.View,
            ActionId: req.ActionId,
            TaskId: req.TaskId,
            ProjectId: req.ProjectId,
            RiskId: req.RiskId,
            TeamMemberId: req.TeamMemberId), ct);

        await Send.ResponseAsync(new CreateAiConversationResponse(result.ConversationId, result.TurnSessionId), 202, ct);
    }
}
