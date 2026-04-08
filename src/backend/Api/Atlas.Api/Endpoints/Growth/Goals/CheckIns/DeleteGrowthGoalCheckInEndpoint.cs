using Atlas.Api.DTOs.Growth.Goals.CheckIns;
using Atlas.Application.Features.Growth.Goals.CheckIns.DeleteGrowthGoalCheckIn;

namespace Atlas.Api.Endpoints.Growth.Goals.CheckIns;

public sealed class DeleteGrowthGoalCheckInEndpoint : Endpoint<DeleteGrowthGoalCheckInRequest>
{
    private readonly IMediator _mediator;

    public DeleteGrowthGoalCheckInEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/growth/{growthId:guid}/goals/{goalId:guid}/check-ins/{checkInId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Delete a check-in from a growth goal"; });
    }

    public override async Task HandleAsync(DeleteGrowthGoalCheckInRequest req, CancellationToken ct)
    {
        Guid growthId = Route<Guid>("growthId");
        Guid goalId = Route<Guid>("goalId");
        Guid checkInId = Route<Guid>("checkInId");
        req = new DeleteGrowthGoalCheckInRequest(GrowthId: growthId, GoalId: goalId, CheckInId: checkInId);

        var ok = await _mediator.Send(new DeleteGrowthGoalCheckInCommand(req.GrowthId, req.GoalId, req.CheckInId), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

