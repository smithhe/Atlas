using Atlas.Application.Abstractions.AzureDevOps;

namespace Atlas.Application.Features.AzureDevOps.Projects;

public sealed record ListAzureProjectsQuery(string Organization) : IRequest<IReadOnlyList<AzureProjectSummary>>;
