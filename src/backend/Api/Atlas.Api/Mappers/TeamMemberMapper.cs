using Atlas.Api.DTOs.TeamMembers;
using Atlas.Domain.Entities;

namespace Atlas.Api.Mappers;

internal static class TeamMemberMapper
{
    public static TeamMemberListItemDto ToListItemDto(TeamMember m)
    {
        return new TeamMemberListItemDto(m.Id, m.Name, m.Role, m.StatusDot);
    }

    public static TeamMemberDto ToDto(TeamMember m)
    {
        var profile = new TeamMemberProfileDto(m.Profile.TimeZone, m.Profile.TypicalHours);
        var signals = new TeamMemberSignalsDto(m.Signals.Load, m.Signals.Delivery, m.Signals.SupportNeeded);

        var notes = m.Notes
            .OrderBy(n => n.PinnedOrder ?? int.MaxValue)
            .ThenByDescending(n => n.CreatedAt)
            .Select(n => new TeamNoteDto(
                n.Id,
                n.CreatedAt,
                n.LastModifiedAt,
                n.Type,
                n.Title,
                n.Text,
                n.PinnedOrder))
            .ToList();

        var risks = m.Risks
            .OrderByDescending(r => r.LastReviewedAt ?? DateTimeOffset.MinValue)
            .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
            .Select(r => new TeamMemberRiskDto(
                r.Id,
                r.Title,
                r.Severity,
                r.RiskType,
                r.Status,
                r.Trend,
                r.FirstNoticedDate,
                r.ImpactArea,
                r.Description,
                r.CurrentAction,
                r.LastReviewedAt,
                r.LinkedGlobalRiskId))
            .ToList();

        var localNotesByWorkItemId = m.AzureWorkItemLocalNotes
            .GroupBy(x => x.WorkItemId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(n => n.CreatedAt)
                    .Select(n => new TeamMemberAzureWorkItemLocalNoteDto(n.Id, n.CreatedAt, n.Text))
                    .ToList());

        var azureWorkItems = m.AzureWorkItemLinks
            .Where(x => x.AzureWorkItem is not null)
            .OrderByDescending(x => x.AzureWorkItem!.ChangedDateUtc)
            .Select(x =>
            {
                var workItemId = x.AzureWorkItem!.WorkItemId;
                localNotesByWorkItemId.TryGetValue(workItemId, out List<TeamMemberAzureWorkItemLocalNoteDto>? teamMemberAzureWorkItemLocalNoteDtos);
                return new TeamMemberAzureWorkItemDto(
                    workItemId.ToString(),
                    x.AzureWorkItem.Title,
                    x.AzureWorkItem.State,
                    x.AzureWorkItem.AssignedToUniqueName,
                    x.AzureWorkItem.Url,
                    x.ProjectId,
                    teamMemberAzureWorkItemLocalNoteDtos ?? []);
            })
            .ToList();

        var projectIds = m.Projects
            .Select(p => p.ProjectId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var linkedGlobalRiskIds = m.LinkedRisks
            .Select(x => x.RiskId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        return new TeamMemberDto(
            m.Id,
            m.Name,
            m.Role,
            m.StatusDot,
            m.CurrentFocus,
            profile,
            signals,
            notes,
            risks,
            azureWorkItems,
            projectIds,
            linkedGlobalRiskIds);
    }
}

