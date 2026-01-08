using Atlas.Api.DTOs.Growth.Goals.CheckIns;
using Atlas.Application.Features.Growth.Goals.CheckIns.AddGrowthGoalCheckIn;

namespace Atlas.Api.Endpoints.Growth.Goals.CheckIns;

public sealed class AddGrowthGoalCheckInEndpoint : Endpoint<AddGrowthGoalCheckInRequest, AddGrowthGoalCheckInResponse>
{
    private readonly IMediator _mediator;

    public AddGrowthGoalCheckInEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/growth/{growthId:guid}/goals/{goalId:guid}/check-ins");
        AllowAnonymous();
        Summary(s => { s.Summary = "Add a check-in to a growth goal"; });
    }

    public override async Task HandleAsync(AddGrowthGoalCheckInRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        var goalId = Route<Guid>("goalId");
        req = req with { GrowthId = growthId, GoalId = goalId };

        var id = await _mediator.Send(new AddGrowthGoalCheckInCommand(req.GrowthId, req.GoalId, req.Date, req.Signal, req.Note), ct);
        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new AddGrowthGoalCheckInResponse(id), 201, ct);
    }
}

