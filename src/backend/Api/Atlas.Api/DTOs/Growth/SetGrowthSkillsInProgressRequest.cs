namespace Atlas.Api.DTOs.Growth;

public sealed record SetGrowthSkillsInProgressRequest(Guid GrowthId, IReadOnlyList<string> SkillsInProgress);

