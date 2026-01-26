using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.Users;

public sealed class ListImportedAzureUsersQueryHandler : IRequestHandler<ListImportedAzureUsersQuery, IReadOnlyList<AzureUser>>
{
    private readonly IAzureUserRepository _azureUsers;

    public ListImportedAzureUsersQueryHandler(IAzureUserRepository azureUsers)
    {
        _azureUsers = azureUsers;
    }

    public async Task<IReadOnlyList<AzureUser>> Handle(ListImportedAzureUsersQuery request, CancellationToken cancellationToken)
    {
        return await _azureUsers.ListActiveAsync(cancellationToken);
    }
}
