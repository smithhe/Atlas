namespace Atlas.Application.Features.AzureDevOps.ProductOwners;

public sealed record AzureProductOwnerSelection(
    string DisplayName,
    string UniqueName,
    string? Descriptor);

public sealed record ImportAzureProductOwnersCommand(
    IReadOnlyList<AzureProductOwnerSelection> Users) : IRequest<ImportAzureProductOwnersResult>;
