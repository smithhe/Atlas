using Atlas.Api.DTOs.Growth.FeedbackThemes;
using Atlas.Application.Features.Growth.FeedbackThemes.DeleteFeedbackTheme;

namespace Atlas.Api.Endpoints.Growth.FeedbackThemes;

public sealed class DeleteFeedbackThemeEndpoint : Endpoint<DeleteFeedbackThemeRequest>
{
    private readonly IMediator _mediator;

    public DeleteFeedbackThemeEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/growth/{growthId:guid}/feedback-themes/{themeId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Delete a feedback theme from a growth plan"; });
    }

    public override async Task HandleAsync(DeleteFeedbackThemeRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        var themeId = Route<Guid>("themeId");
        req = req with { GrowthId = growthId, ThemeId = themeId };

        var ok = await _mediator.Send(new DeleteFeedbackThemeCommand(req.GrowthId, req.ThemeId), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

