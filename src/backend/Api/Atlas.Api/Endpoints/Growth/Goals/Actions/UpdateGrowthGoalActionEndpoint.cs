using Atlas.Api.DTOs.Growth.Goals.Actions;
using Atlas.Application.Features.Growth.Goals.Actions.UpdateGrowthGoalAction;

namespace Atlas.Api.Endpoints.Growth.Goals.Actions;

public sealed class UpdateGrowthGoalActionEndpoint : Endpoint<UpdateGrowthGoalActionRequest>
{
    private readonly IMediator _mediator;

    public UpdateGrowthGoalActionEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/growth/{growthId:guid}/goals/{goalId:guid}/actions/{actionId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update an action on a growth goal"; });
    }

    public override async Task HandleAsync(UpdateGrowthGoalActionRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        var goalId = Route<Guid>("goalId");
        var actionId = Route<Guid>("actionId");
        req = req with { GrowthId = growthId, GoalId = goalId, ActionId = actionId };

        var ok = await _mediator.Send(new UpdateGrowthGoalActionCommand(
            req.GrowthId,
            req.GoalId,
            req.ActionId,
            req.Title,
            req.State,
            req.DueDate,
            req.Priority,
            req.Notes,
            req.Evidence), ct);

        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

