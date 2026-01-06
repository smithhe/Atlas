using Atlas.Api.DTOs.Settings;
using Atlas.Application.Features.Settings.UpdateSettings;

namespace Atlas.Api.Endpoints.Settings;

public sealed class UpdateSettingsEndpoint : Endpoint<UpdateSettingsRequest>
{
    private readonly IMediator _mediator;

    public UpdateSettingsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/settings");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update application settings (singleton)"; });
    }

    public override async Task HandleAsync(UpdateSettingsRequest req, CancellationToken ct)
    {
        await _mediator.Send(new UpdateSettingsCommand(req.StaleDays, req.DefaultAiManualOnly, req.Theme, req.AzureDevOpsBaseUrl), ct);
        await Send.NoContentAsync(ct);
    }
}

