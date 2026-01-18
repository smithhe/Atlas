namespace Atlas.Api.DTOs.AzureDevOps;

public sealed record AzureTeamAreaPathDto(string Value, bool IncludeChildren);

public sealed record AzureTeamAreaPathsDto(string? DefaultValue, IReadOnlyList<AzureTeamAreaPathDto> Values);

