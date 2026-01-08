namespace Atlas.Api.DTOs.Growth;

public sealed record GrowthFeedbackThemeDto(
    Guid Id,
    string Title,
    string Description,
    string? ObservedSinceLabel);

