using Atlas.Api.DTOs.Growth.Goals;
using Atlas.Application.Features.Growth.Goals.AddGrowthGoal;

namespace Atlas.Api.Endpoints.Growth.Goals;

public sealed class AddGrowthGoalEndpoint : Endpoint<AddGrowthGoalRequest, AddGrowthGoalResponse>
{
    private readonly IMediator _mediator;

    public AddGrowthGoalEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/growth/{growthId:guid}/goals");
        AllowAnonymous();
        Summary(s => { s.Summary = "Add a goal to a growth plan"; });
    }

    public override async Task HandleAsync(AddGrowthGoalRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        req = req with { GrowthId = growthId };

        var id = await _mediator.Send(new AddGrowthGoalCommand(
            req.GrowthId,
            req.Title,
            req.Description,
            req.Status,
            req.StartDate,
            req.TargetDate,
            req.Category,
            req.Priority), ct);

        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new AddGrowthGoalResponse(id), 201, ct);
    }
}

