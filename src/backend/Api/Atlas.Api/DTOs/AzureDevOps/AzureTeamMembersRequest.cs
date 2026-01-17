namespace Atlas.Api.DTOs.AzureDevOps;

public sealed class AzureTeamMembersRequest
{
    [QueryParam]
    public string Organization { get; set; } = string.Empty;

    [QueryParam]
    public string ProjectId { get; set; } = string.Empty;

    [QueryParam]
    public string TeamId { get; set; } = string.Empty;
}
