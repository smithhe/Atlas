using Atlas.Api.DTOs.Settings;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Settings.GetSettings;

namespace Atlas.Api.Endpoints.Settings;

public sealed class GetSettingsEndpoint : EndpointWithoutRequest<SettingsDto>
{
    private readonly IMediator _mediator;

    public GetSettingsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/settings");
        AllowAnonymous();
        Summary(s => { s.Summary = "Get application settings (singleton)"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var settings = await _mediator.Send(new GetSettingsQuery(), ct);
        await Send.OkAsync(SettingsMapper.ToDto(settings), ct);
    }
}

