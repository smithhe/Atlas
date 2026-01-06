using Atlas.Api.DTOs.Projects;
using Atlas.Domain.Entities;

namespace Atlas.Api.Mappers;

internal static class ProjectMapper
{
    public static ProjectListItemDto ToListItemDto(Project p)
    {
        return new ProjectListItemDto(
            p.Id,
            p.Name,
            p.Summary,
            p.Status,
            p.Health,
            p.TargetDate,
            p.Priority,
            p.ProductOwnerId,
            p.LastUpdatedAt);
    }

    public static ProjectDto ToDto(Project p)
    {
        var tags = (p.Tags ?? [])
            .OrderBy(t => t.Value, StringComparer.OrdinalIgnoreCase)
            .Select(t => new ProjectTagDto(t.Value))
            .ToList();

        var links = (p.Links ?? [])
            .OrderBy(l => l.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.Url, StringComparer.OrdinalIgnoreCase)
            .Select(l => new ProjectLinkDto(l.Label, l.Url))
            .ToList();

        var taskIds = (p.Tasks ?? [])
            .Select(t => t.Id)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var riskIds = (p.Risks ?? [])
            .Select(r => r.Id)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var memberIds = (p.TeamMembers ?? [])
            .Select(tm => tm.TeamMemberId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        return new ProjectDto(
            p.Id,
            p.Name,
            p.Summary,
            p.Description,
            p.Status,
            p.Health,
            p.TargetDate,
            p.Priority,
            p.ProductOwnerId,
            tags,
            links,
            p.LastUpdatedAt,
            taskIds,
            riskIds,
            memberIds);
    }
}

