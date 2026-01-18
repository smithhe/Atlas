using Atlas.Application.Abstractions.AzureDevOps;

namespace Atlas.Application.Features.AzureDevOps.Areas;

public sealed record ListAzureTeamAreaPathsQuery(string Organization, string ProjectId, string TeamName) : IRequest<AzureTeamAreaPaths>;

