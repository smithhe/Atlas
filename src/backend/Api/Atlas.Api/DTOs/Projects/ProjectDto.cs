using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Projects;

public sealed record ProjectLinkDto(string Label, string Url);
public sealed record ProjectTagDto(string Value);

public sealed record ProjectListItemDto(
    Guid Id,
    string Name,
    string Summary,
    ProjectStatus? Status,
    HealthSignal? Health,
    DateOnly? TargetDate,
    Priority? Priority,
    Guid? ProductOwnerId,
    DateTimeOffset? LastUpdatedAt);

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Summary,
    string? Description,
    ProjectStatus? Status,
    HealthSignal? Health,
    DateOnly? TargetDate,
    Priority? Priority,
    Guid? ProductOwnerId,
    IReadOnlyList<ProjectTagDto> Tags,
    IReadOnlyList<ProjectLinkDto> Links,
    DateTimeOffset? LastUpdatedAt,
    IReadOnlyList<Guid> LinkedTaskIds,
    IReadOnlyList<Guid> LinkedRiskIds,
    IReadOnlyList<Guid> TeamMemberIds);

