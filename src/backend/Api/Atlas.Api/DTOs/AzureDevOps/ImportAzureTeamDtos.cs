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

public sealed record ImportAzureProductOwnersRequest(IReadOnlyList<AzureUserSelectionDto> Users);

public sealed record ImportAzureProductOwnersResultDto(
    int UsersAdded,
    int UsersUpdated,
    int ProductOwnersCreated,
    int MappingsCreated);
