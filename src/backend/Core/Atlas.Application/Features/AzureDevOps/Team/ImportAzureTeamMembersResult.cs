namespace Atlas.Application.Features.AzureDevOps.Team;

public sealed record ImportAzureTeamMembersResult(
    int UsersAdded,
    int UsersUpdated,
    int TeamMembersCreated,
    int MappingsCreated);
