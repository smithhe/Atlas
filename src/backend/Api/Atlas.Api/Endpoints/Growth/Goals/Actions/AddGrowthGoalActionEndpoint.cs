using Atlas.Api.DTOs.Growth.Goals.Actions;
using Atlas.Application.Features.Growth.Goals.Actions.AddGrowthGoalAction;

namespace Atlas.Api.Endpoints.Growth.Goals.Actions;

public sealed class AddGrowthGoalActionEndpoint : Endpoint<AddGrowthGoalActionRequest, AddGrowthGoalActionResponse>
{
    private readonly IMediator _mediator;

    public AddGrowthGoalActionEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/growth/{growthId:guid}/goals/{goalId:guid}/actions");
        AllowAnonymous();
        Summary(s => { s.Summary = "Add an action to a growth goal"; });
    }

    public override async Task HandleAsync(AddGrowthGoalActionRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        var goalId = Route<Guid>("goalId");
        req = req with { GrowthId = growthId, GoalId = goalId };

        var id = await _mediator.Send(new AddGrowthGoalActionCommand(
            req.GrowthId,
            req.GoalId,
            req.Title,
            req.State,
            req.DueDate,
            req.Priority,
            req.Notes,
            req.Evidence), ct);

        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new AddGrowthGoalActionResponse(id), 201, ct);
    }
}

