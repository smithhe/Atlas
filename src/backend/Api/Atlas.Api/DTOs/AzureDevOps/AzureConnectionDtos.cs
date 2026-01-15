namespace Atlas.Api.DTOs.AzureDevOps;

public sealed record AzureConnectionDto(
    string Organization,
    string Project,
    string AreaPath,
    string? TeamName,
    bool IsEnabled);

public sealed record UpdateAzureConnectionRequest(
    string Organization,
    string Project,
    string AreaPath,
    string? TeamName,
    bool IsEnabled);
