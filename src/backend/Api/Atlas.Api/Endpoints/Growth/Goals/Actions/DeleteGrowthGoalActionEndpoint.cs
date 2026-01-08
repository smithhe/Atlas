using Atlas.Api.DTOs.Growth.Goals.Actions;
using Atlas.Application.Features.Growth.Goals.Actions.DeleteGrowthGoalAction;

namespace Atlas.Api.Endpoints.Growth.Goals.Actions;

public sealed class DeleteGrowthGoalActionEndpoint : Endpoint<DeleteGrowthGoalActionRequest>
{
    private readonly IMediator _mediator;

    public DeleteGrowthGoalActionEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/growth/{growthId:guid}/goals/{goalId:guid}/actions/{actionId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Delete an action from a growth goal"; });
    }

    public override async Task HandleAsync(DeleteGrowthGoalActionRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        var goalId = Route<Guid>("goalId");
        var actionId = Route<Guid>("actionId");
        req = req with { GrowthId = growthId, GoalId = goalId, ActionId = actionId };

        var ok = await _mediator.Send(new DeleteGrowthGoalActionCommand(req.GrowthId, req.GoalId, req.ActionId), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

