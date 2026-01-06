using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Projects;

public sealed record UpdateProjectRequest(
    string Name,
    string Summary,
    string? Description,
    ProjectStatus? Status,
    HealthSignal? Health,
    DateOnly? TargetDate,
    Priority? Priority,
    Guid? ProductOwnerId,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<ProjectLinkDto>? Links = null);

