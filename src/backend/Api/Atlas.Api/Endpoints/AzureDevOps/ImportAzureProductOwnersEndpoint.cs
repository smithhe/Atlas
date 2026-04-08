using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.ProductOwners;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class ImportAzureProductOwnersEndpoint
    : Endpoint<ImportAzureProductOwnersRequest, ImportAzureProductOwnersResultDto>
{
    private readonly IMediator _mediator;

    public ImportAzureProductOwnersEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/azure-devops/product-owners/import");
        AllowAnonymous();
        Summary(s => { s.Summary = "Import selected Azure users into local product owners"; });
    }

    public override async Task HandleAsync(ImportAzureProductOwnersRequest req, CancellationToken ct)
    {
        var selections = req.Users
            .Select(u => new AzureProductOwnerSelection(u.DisplayName, u.UniqueName, u.Descriptor))
            .ToList();

        ImportAzureProductOwnersResult result = await _mediator.Send(new ImportAzureProductOwnersCommand(selections), ct);

        await Send.OkAsync(new ImportAzureProductOwnersResultDto(
            result.UsersAdded,
            result.UsersUpdated,
            result.ProductOwnersCreated,
            result.MappingsCreated), ct);
    }
}
