using Atlas.Api.DTOs.Growth.FeedbackThemes;
using Atlas.Application.Features.Growth.FeedbackThemes.UpdateFeedbackTheme;

namespace Atlas.Api.Endpoints.Growth.FeedbackThemes;

public sealed class UpdateFeedbackThemeEndpoint : Endpoint<UpdateFeedbackThemeRequest>
{
    private readonly IMediator _mediator;

    public UpdateFeedbackThemeEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/growth/{growthId:guid}/feedback-themes/{themeId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update a feedback theme on a growth plan"; });
    }

    public override async Task HandleAsync(UpdateFeedbackThemeRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        var themeId = Route<Guid>("themeId");
        req = req with { GrowthId = growthId, ThemeId = themeId };

        var ok = await _mediator.Send(new UpdateFeedbackThemeCommand(req.GrowthId, req.ThemeId, req.Title, req.Description, req.ObservedSinceLabel), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

