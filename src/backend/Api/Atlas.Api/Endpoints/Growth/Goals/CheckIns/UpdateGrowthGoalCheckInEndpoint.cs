using Atlas.Api.DTOs.Growth.Goals.CheckIns;
using Atlas.Application.Features.Growth.Goals.CheckIns.UpdateGrowthGoalCheckIn;

namespace Atlas.Api.Endpoints.Growth.Goals.CheckIns;

public sealed class UpdateGrowthGoalCheckInEndpoint : Endpoint<UpdateGrowthGoalCheckInRequest>
{
    private readonly IMediator _mediator;

    public UpdateGrowthGoalCheckInEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/growth/{growthId:guid}/goals/{goalId:guid}/check-ins/{checkInId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update a growth goal check-in"; });
    }

    public override async Task HandleAsync(UpdateGrowthGoalCheckInRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        var goalId = Route<Guid>("goalId");
        var checkInId = Route<Guid>("checkInId");
        req = req with { GrowthId = growthId, GoalId = goalId, CheckInId = checkInId };

        var ok = await _mediator.Send(new UpdateGrowthGoalCheckInCommand(req.GrowthId, req.GoalId, req.CheckInId, req.Date, req.Signal, req.Note), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

