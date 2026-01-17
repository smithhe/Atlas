namespace Atlas.Api.DTOs.AzureDevOps;

public sealed class AzureOrganizationRequest
{
    [QueryParam]
    public string Organization { get; set; } = string.Empty;
}
