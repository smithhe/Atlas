using Atlas.Application.Abstractions.AzureDevOps;

namespace Atlas.Application.Features.AzureDevOps.Users;

public sealed record ListAzureUsersQuery(string Organization, string ProjectId, string TeamId) : IRequest<IReadOnlyList<AzureUserSummary>>;
