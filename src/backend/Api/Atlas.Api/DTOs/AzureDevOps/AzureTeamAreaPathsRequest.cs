namespace Atlas.Api.DTOs.AzureDevOps;

public sealed class AzureTeamAreaPathsRequest
{
    [QueryParam]
    public string Organization { get; set; } = string.Empty;

    [QueryParam]
    public string ProjectId { get; set; } = string.Empty;

    [QueryParam]
    public string TeamName { get; set; } = string.Empty;
}

