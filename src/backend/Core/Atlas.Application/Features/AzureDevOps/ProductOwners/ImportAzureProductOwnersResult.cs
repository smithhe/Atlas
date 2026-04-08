namespace Atlas.Application.Features.AzureDevOps.ProductOwners;

public sealed record ImportAzureProductOwnersResult(
    int UsersAdded,
    int UsersUpdated,
    int ProductOwnersCreated,
    int MappingsCreated);
