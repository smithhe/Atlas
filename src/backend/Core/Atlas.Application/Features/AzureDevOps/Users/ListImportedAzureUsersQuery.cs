using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.Users;

public sealed record ListImportedAzureUsersQuery() : IRequest<IReadOnlyList<AzureUser>>;
