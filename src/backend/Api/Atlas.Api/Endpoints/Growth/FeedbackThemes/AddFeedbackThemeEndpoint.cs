using Atlas.Api.DTOs.Growth.FeedbackThemes;
using Atlas.Application.Features.Growth.FeedbackThemes.AddFeedbackTheme;

namespace Atlas.Api.Endpoints.Growth.FeedbackThemes;

public sealed class AddFeedbackThemeEndpoint : Endpoint<AddFeedbackThemeRequest, AddFeedbackThemeResponse>
{
    private readonly IMediator _mediator;

    public AddFeedbackThemeEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/growth/{growthId:guid}/feedback-themes");
        AllowAnonymous();
        Summary(s => { s.Summary = "Add a feedback theme to a growth plan"; });
    }

    public override async Task HandleAsync(AddFeedbackThemeRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        req = req with { GrowthId = growthId };

        var id = await _mediator.Send(new AddFeedbackThemeCommand(req.GrowthId, req.Title, req.Description, req.ObservedSinceLabel), ct);
        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new AddFeedbackThemeResponse(id), 201, ct);
    }
}

