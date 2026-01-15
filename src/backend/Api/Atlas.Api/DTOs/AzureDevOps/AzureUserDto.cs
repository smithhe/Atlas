namespace Atlas.Api.DTOs.AzureDevOps;

public sealed record AzureUserDto(string DisplayName, string UniqueName, string? Descriptor);
