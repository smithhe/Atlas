namespace Atlas.Application.Features.AzureDevOps.Team;

public sealed record AzureTeamMemberSelection(
    string DisplayName,
    string UniqueName,
    string? Descriptor);

public sealed record ImportAzureTeamMembersCommand(IReadOnlyList<AzureTeamMemberSelection> Users) : IRequest<ImportAzureTeamMembersResult>;
