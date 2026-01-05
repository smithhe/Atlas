namespace Atlas.Application.Features.Growth.Skills.SetGrowthSkillsInProgress;

public sealed record SetGrowthSkillsInProgressCommand(Guid GrowthId, IReadOnlyList<string> SkillsInProgress) : IRequest<bool>;

