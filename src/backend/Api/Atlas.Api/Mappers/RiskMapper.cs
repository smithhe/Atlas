using Atlas.Api.DTOs.Risks;
using Atlas.Domain.Entities;

namespace Atlas.Api.Mappers;

internal static class RiskMapper
{
    public static RiskListItemDto ToListItemDto(Risk risk)
    {
        return new RiskListItemDto(
            risk.Id,
            risk.Title,
            risk.Status,
            risk.Severity,
            risk.ProjectId,
            risk.LastUpdatedAt);
    }

    public static RiskDto ToDto(Risk risk)
    {
        var linkedTaskIds = (risk.Tasks ?? [])
            .Select(t => t.Id)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var linkedTeamMemberIds = (risk.LinkedTeamMembers ?? [])
            .Select(x => x.TeamMemberId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var history = (risk.History ?? [])
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new RiskHistoryEntryDto(h.Id, h.Text, h.CreatedAt))
            .ToList();

        return new RiskDto(
            risk.Id,
            risk.Title,
            risk.Status,
            risk.Severity,
            risk.ProjectId,
            risk.Description,
            risk.Evidence,
            linkedTaskIds,
            linkedTeamMemberIds,
            history,
            risk.LastUpdatedAt);
    }
}

