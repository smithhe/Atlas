using Atlas.Ui.Models;

namespace Atlas.Ui.Mapping;

public static class EntityClone
{
    public static AtlasTask Task(AtlasTask t, string? title = null, Priority? priority = null, Models.TaskStatus? status = null,
        bool setStatus = false, Guid? assigneeId = null, bool setAssignee = false, string? project = null, bool setProject = false,
        string? risk = null, bool setRisk = false, string? dueDate = null, bool setDueDate = false,
        IReadOnlyList<Guid>? dependencyTaskIds = null, string? estimatedDurationText = null,
        Confidence? estimateConfidence = null, string? actualDurationText = null, bool setActual = false,
        string? notes = null, string? lastTouchedIso = null) =>
        new()
        {
            Id = t.Id,
            Title = title ?? t.Title,
            Priority = priority ?? t.Priority,
            Status = setStatus ? status : t.Status,
            AssigneeId = setAssignee ? assigneeId : t.AssigneeId,
            Project = setProject ? project : t.Project,
            Risk = setRisk ? risk : t.Risk,
            DueDate = setDueDate ? dueDate : t.DueDate,
            DependencyTaskIds = dependencyTaskIds ?? t.DependencyTaskIds,
            EstimatedDurationText = estimatedDurationText ?? t.EstimatedDurationText,
            EstimateConfidence = estimateConfidence ?? t.EstimateConfidence,
            ActualDurationText = setActual ? actualDurationText : t.ActualDurationText,
            Notes = notes ?? t.Notes,
            LastTouchedIso = lastTouchedIso ?? t.LastTouchedIso
        };

    public static Risk Risk(Risk r, string? title = null, RiskStatus? status = null, string? severity = null,
        string? project = null, bool setProject = false, string? description = null, string? evidence = null,
        IReadOnlyList<Guid>? linkedTaskIds = null, IReadOnlyList<Guid>? linkedTeamMemberIds = null,
        IReadOnlyList<RiskHistoryEntry>? history = null, string? lastUpdatedIso = null) =>
        new()
        {
            Id = r.Id,
            Title = title ?? r.Title,
            Status = status ?? r.Status,
            Severity = severity ?? r.Severity,
            Project = setProject ? project : r.Project,
            OwnerId = r.OwnerId,
            Description = description ?? r.Description,
            Evidence = evidence ?? r.Evidence,
            LinkedTaskIds = linkedTaskIds ?? r.LinkedTaskIds,
            LinkedTeamMemberIds = linkedTeamMemberIds ?? r.LinkedTeamMemberIds,
            History = history ?? r.History,
            LastUpdatedIso = lastUpdatedIso ?? r.LastUpdatedIso
        };

    public static Project Project(Project p, string? name = null, string? summary = null, string? description = null,
        bool setDescription = false, ProjectStatus? status = null, HealthSignal? health = null,
        string? targetDateIso = null, bool setTarget = false, Priority? priority = null, bool setPriority = false,
        Guid? productOwnerId = null, bool setOwner = false, IReadOnlyList<string>? tags = null,
        IReadOnlyList<ProjectLink>? links = null, ProjectCheckIn? latestCheckIn = null, bool setLatestCheckIn = false,
        string? lastUpdatedIso = null,
        IReadOnlyList<Guid>? linkedTaskIds = null, IReadOnlyList<Guid>? linkedRiskIds = null,
        IReadOnlyList<Guid>? teamMemberIds = null) =>
        new()
        {
            Id = p.Id,
            Name = name ?? p.Name,
            Summary = summary ?? p.Summary,
            Description = setDescription ? description : p.Description,
            Status = status ?? p.Status,
            Health = health ?? p.Health,
            TargetDateIso = setTarget ? targetDateIso : p.TargetDateIso,
            Priority = setPriority ? priority : p.Priority,
            ProductOwnerId = setOwner ? productOwnerId : p.ProductOwnerId,
            Tags = tags ?? p.Tags,
            Links = links ?? p.Links,
            LatestCheckIn = setLatestCheckIn ? latestCheckIn : p.LatestCheckIn,
            LastUpdatedIso = lastUpdatedIso ?? p.LastUpdatedIso,
            LinkedTaskIds = linkedTaskIds ?? p.LinkedTaskIds,
            LinkedRiskIds = linkedRiskIds ?? p.LinkedRiskIds,
            TeamMemberIds = teamMemberIds ?? p.TeamMemberIds
        };
}
