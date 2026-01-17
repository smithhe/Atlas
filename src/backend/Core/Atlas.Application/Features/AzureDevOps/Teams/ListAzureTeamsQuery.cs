using Atlas.Application.Abstractions.AzureDevOps;

namespace Atlas.Application.Features.AzureDevOps.Teams;

public sealed record ListAzureTeamsQuery(string Organization, string ProjectId) : IRequest<IReadOnlyList<AzureTeamSummary>>;
