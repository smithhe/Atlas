using Atlas.Api.DTOs.Growth.Goals;
using Atlas.Application.Features.Growth.Goals.UpdateGrowthGoal;

namespace Atlas.Api.Endpoints.Growth.Goals;

public sealed class UpdateGrowthGoalEndpoint : Endpoint<UpdateGrowthGoalRequest>
{
    private readonly IMediator _mediator;

    public UpdateGrowthGoalEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/growth/{growthId:guid}/goals/{goalId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update a growth goal"; });
    }

    public override async Task HandleAsync(UpdateGrowthGoalRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        var goalId = Route<Guid>("goalId");
        req = req with { GrowthId = growthId, GoalId = goalId };

        var successCriteria = req.SuccessCriteria is null
            ? null
            : string.Join("\n", req.SuccessCriteria.Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)));

        var ok = await _mediator.Send(new UpdateGrowthGoalCommand(
            req.GrowthId,
            req.GoalId,
            req.Title,
            req.Description,
            req.Status,
            req.StartDate,
            req.TargetDate,
            req.Category,
            req.Priority,
            req.ProgressPercent,
            req.Summary,
            successCriteria), ct);

        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

