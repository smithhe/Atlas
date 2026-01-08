using Atlas.Api.DTOs.Growth.Goals;
using Atlas.Application.Features.Growth.Goals.DeleteGrowthGoal;

namespace Atlas.Api.Endpoints.Growth.Goals;

public sealed class DeleteGrowthGoalEndpoint : Endpoint<DeleteGrowthGoalRequest>
{
    private readonly IMediator _mediator;

    public DeleteGrowthGoalEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/growth/{growthId:guid}/goals/{goalId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Delete a growth goal"; });
    }

    public override async Task HandleAsync(DeleteGrowthGoalRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        var goalId = Route<Guid>("goalId");
        req = req with { GrowthId = growthId, GoalId = goalId };

        var ok = await _mediator.Send(new DeleteGrowthGoalCommand(req.GrowthId, req.GoalId), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

