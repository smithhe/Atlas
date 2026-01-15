namespace Atlas.Api.DTOs.AzureDevOps;

public sealed record AzureUserSelectionDto(
    string DisplayName,
    string UniqueName,
    string? Descriptor);

public sealed record ImportAzureTeamRequest(IReadOnlyList<AzureUserSelectionDto> Users);

public sealed record ImportAzureTeamResultDto(
    int UsersAdded,
    int UsersUpdated,
    int TeamMembersCreated,
    int MappingsCreated);
