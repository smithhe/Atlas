using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.Users;

public sealed class ListImportedAzureUsersQueryHandler : IRequestHandler<ListImportedAzureUsersQuery, IReadOnlyList<AzureUser>>
{
    private readonly IAzureUserRepository _azureUsers;
    private readonly IAzureUserMappingRepository _teamMappings;
    private readonly IAzureProductOwnerMappingRepository _productOwnerMappings;

    public ListImportedAzureUsersQueryHandler(
        IAzureUserRepository azureUsers,
        IAzureUserMappingRepository teamMappings,
        IAzureProductOwnerMappingRepository productOwnerMappings)
    {
        _azureUsers = azureUsers;
        _teamMappings = teamMappings;
        _productOwnerMappings = productOwnerMappings;
    }

    public async Task<IReadOnlyList<AzureUser>> Handle(ListImportedAzureUsersQuery request, CancellationToken cancellationToken)
    {
        var teamMapped = await _teamMappings.ListUniqueNamesAsync(cancellationToken);
        var productOwnerMapped = await _productOwnerMappings.ListUniqueNamesAsync(cancellationToken);
        var mappedUniqueNames = teamMapped
            .Concat(productOwnerMapped)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return await _azureUsers.GetByUniqueNamesAsync(mappedUniqueNames, cancellationToken);
    }
}
