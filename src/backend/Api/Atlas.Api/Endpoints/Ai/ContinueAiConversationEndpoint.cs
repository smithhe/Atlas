using Atlas.Api.DTOs.Ai;
using Atlas.Application.Features.Ai.ContinueConversation;

namespace Atlas.Api.Endpoints.Ai;

public sealed class ContinueAiConversationEndpoint : Endpoint<ContinueAiConversationRequest, ContinueAiConversationResponse>
{
    private readonly IMediator _mediator;

    public ContinueAiConversationEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/ai/conversations/{conversationId:guid}/messages");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Continue an AI conversation with a new message";
            s.Response<ContinueAiConversationResponse>(202, "Accepted");
        });
    }

    public override async Task HandleAsync(ContinueAiConversationRequest req, CancellationToken ct)
    {
        Guid conversationId = Route<Guid>("conversationId");

        try
        {
            ContinueAiConversationResult result = await _mediator.Send(
                new ContinueAiConversationCommand(conversationId, req.Prompt),
                ct);

            await Send.ResponseAsync(new ContinueAiConversationResponse(result.TurnSessionId), 202, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(409, ct);
        }
    }
}
