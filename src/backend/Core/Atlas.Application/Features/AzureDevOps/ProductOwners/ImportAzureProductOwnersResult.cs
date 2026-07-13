namespace Atlas.Application.Features.AzureDevOps.ProductOwners;

public sealed record ReusedProductOwnerName(
    string DisplayName,
    string AzureUniqueName,
    Guid ExistingProductOwnerId);

public sealed record ImportAzureProductOwnersResult(
    int UsersAdded,
    int UsersUpdated,
    int ProductOwnersCreated,
    int MappingsCreated,
    IReadOnlyList<ReusedProductOwnerName> ReusedProductOwnerNames);
