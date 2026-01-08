using Atlas.Api.DTOs.Growth;
using Atlas.Application.Features.Growth.Skills.SetGrowthSkillsInProgress;

namespace Atlas.Api.Endpoints.Growth;

public sealed class SetGrowthSkillsInProgressEndpoint : Endpoint<SetGrowthSkillsInProgressRequest>
{
    private readonly IMediator _mediator;

    public SetGrowthSkillsInProgressEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/growth/{growthId:guid}/skills-in-progress");
        AllowAnonymous();
        Summary(s => { s.Summary = "Set a growth plan's ordered skills-in-progress list"; });
    }

    public override async Task HandleAsync(SetGrowthSkillsInProgressRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        req = req with { GrowthId = growthId };

        var ok = await _mediator.Send(new SetGrowthSkillsInProgressCommand(req.GrowthId, req.SkillsInProgress), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

