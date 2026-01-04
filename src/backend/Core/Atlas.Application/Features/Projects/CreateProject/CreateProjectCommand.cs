using Atlas.Domain.Enums;
using Atlas.Application.DTOs;

namespace Atlas.Application.Features.Projects.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string Summary,
    string? Description,
    ProjectStatus? Status,
    HealthSignal? Health,
    DateOnly? TargetDate,
    Priority? Priority,
    Guid? ProductOwnerId,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<ProjectLinkDto>? Links = null) : IRequest<Guid>;

