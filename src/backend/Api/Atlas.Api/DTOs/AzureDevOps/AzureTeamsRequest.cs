namespace Atlas.Api.DTOs.AzureDevOps;

public sealed class AzureTeamsRequest
{
    [QueryParam]
    public string Organization { get; set; } = string.Empty;

    [QueryParam]
    public string ProjectId { get; set; } = string.Empty;
}
