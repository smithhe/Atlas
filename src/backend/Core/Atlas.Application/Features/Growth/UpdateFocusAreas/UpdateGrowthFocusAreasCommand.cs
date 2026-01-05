namespace Atlas.Application.Features.Growth.UpdateFocusAreas;

public sealed record UpdateGrowthFocusAreasCommand(Guid GrowthId, string FocusAreasMarkdown) : IRequest<bool>;

