using Atlas.Ui.Models;

namespace Atlas.Ui.Mapping;

public static class EntityClone
{
    public static AtlasTask Task(AtlasTask t, string? title = null, Priority? priority = null, Models.TaskStatus? status = null,
        bool setStatus = false, Guid? assigneeId = null, bool setAssignee = false,
        Guid? projectId = null, bool setProjectId = false, string? project = null, bool setProject = false,
        Guid? riskId = null, bool setRiskId = false, string? risk = null, bool setRisk = false, string? dueDate = null, bool setDueDate = false,
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
            ProjectId = setProjectId ? projectId : t.ProjectId,
            Project = setProject ? project : t.Project,
            RiskId = setRiskId ? riskId : t.RiskId,
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
        Guid? projectId = null, bool setProjectId = false, string? project = null, bool setProject = false,
        string? description = null, string? evidence = null,
        IReadOnlyList<Guid>? linkedTaskIds = null, IReadOnlyList<Guid>? linkedTeamMemberIds = null,
        IReadOnlyList<RiskHistoryEntry>? history = null, string? lastUpdatedIso = null) =>
        new()
        {
            Id = r.Id,
            Title = title ?? r.Title,
            Status = status ?? r.Status,
            Severity = severity ?? r.Severity,
            ProjectId = setProjectId ? projectId : r.ProjectId,
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

    public static AzureItem AzureItem(
        AzureItem item,
        string? id = null,
        string? title = null,
        string? status = null,
        string? assignedTo = null,
        bool setAssignedTo = false,
        string? ticketUrl = null,
        bool setTicketUrl = false,
        string? projectId = null,
        bool setProjectId = false,
        string? changedDateUtc = null,
        bool setChangedDateUtc = false,
        string? timeTaken = null,
        bool setTimeTaken = false,
        string? startDateIso = null,
        bool setStartDateIso = false,
        string? commitsUrl = null,
        bool setCommitsUrl = false,
        IReadOnlyList<string>? prUrls = null,
        IReadOnlyList<WorkItemNote>? localNotes = null) =>
        new()
        {
            Id = id ?? item.Id,
            Title = title ?? item.Title,
            Status = status ?? item.Status,
            AssignedTo = setAssignedTo ? assignedTo : item.AssignedTo,
            TicketUrl = setTicketUrl ? ticketUrl : item.TicketUrl,
            ProjectId = setProjectId ? projectId : item.ProjectId,
            ChangedDateUtc = setChangedDateUtc ? changedDateUtc : item.ChangedDateUtc,
            TimeTaken = setTimeTaken ? timeTaken : item.TimeTaken,
            StartDateIso = setStartDateIso ? startDateIso : item.StartDateIso,
            CommitsUrl = setCommitsUrl ? commitsUrl : item.CommitsUrl,
            PrUrls = prUrls ?? item.PrUrls,
            LocalNotes = localNotes ?? item.LocalNotes
        };

    public static TeamMemberRisk TeamMemberRisk(
        TeamMemberRisk r,
        Guid? id = null,
        Guid? memberId = null,
        string? title = null,
        string? severity = null,
        string? riskType = null,
        string? status = null,
        string? trend = null,
        string? firstNoticedDateIso = null,
        string? impactArea = null,
        string? description = null,
        string? currentAction = null,
        string? lastReviewedIso = null,
        bool setLastReviewedIso = false,
        Guid? linkedRiskId = null,
        bool setLinkedRiskId = false) =>
        new()
        {
            Id = id ?? r.Id,
            MemberId = memberId ?? r.MemberId,
            Title = title ?? r.Title,
            Severity = severity ?? r.Severity,
            RiskType = riskType ?? r.RiskType,
            Status = status ?? r.Status,
            Trend = trend ?? r.Trend,
            FirstNoticedDateIso = firstNoticedDateIso ?? r.FirstNoticedDateIso,
            ImpactArea = impactArea ?? r.ImpactArea,
            Description = description ?? r.Description,
            CurrentAction = currentAction ?? r.CurrentAction,
            LastReviewedIso = setLastReviewedIso ? lastReviewedIso : r.LastReviewedIso,
            LinkedRiskId = setLinkedRiskId ? linkedRiskId : r.LinkedRiskId
        };

    public static TeamMember TeamMember(
        TeamMember m,
        string? name = null,
        string? role = null,
        bool setRole = false,
        string? statusDot = null,
        string? currentFocus = null,
        TeamMemberProfile? profile = null,
        TeamMemberSignals? signals = null,
        IReadOnlyList<TeamNote>? notes = null,
        IReadOnlyList<Guid>? pinnedNoteIds = null,
        ActivitySnapshot? activitySnapshot = null,
        IReadOnlyList<AzureItem>? azureItems = null) =>
        new()
        {
            Id = m.Id,
            Name = name ?? m.Name,
            Role = setRole ? role : m.Role,
            StatusDot = statusDot ?? m.StatusDot,
            CurrentFocus = currentFocus ?? m.CurrentFocus,
            Profile = profile ?? m.Profile,
            Signals = signals ?? m.Signals,
            Notes = notes ?? m.Notes,
            PinnedNoteIds = pinnedNoteIds ?? m.PinnedNoteIds,
            ActivitySnapshot = activitySnapshot ?? m.ActivitySnapshot,
            AzureItems = azureItems ?? m.AzureItems
        };

    public static Growth Growth(
        Growth g,
        Guid? id = null,
        Guid? memberId = null,
        IReadOnlyList<GrowthGoal>? goals = null,
        IReadOnlyList<string>? skillsInProgress = null,
        IReadOnlyList<GrowthFeedbackTheme>? feedbackThemes = null,
        string? focusAreasMarkdown = null) =>
        new()
        {
            Id = id ?? g.Id,
            MemberId = memberId ?? g.MemberId,
            Goals = goals ?? g.Goals,
            SkillsInProgress = skillsInProgress ?? g.SkillsInProgress,
            FeedbackThemes = feedbackThemes ?? g.FeedbackThemes,
            FocusAreasMarkdown = focusAreasMarkdown ?? g.FocusAreasMarkdown
        };

    public static Growth ReplaceGoal(Growth growth, Guid goalId, Func<GrowthGoal, GrowthGoal> update) =>
        Growth(growth, goals: growth.Goals.Select(g => g.Id == goalId ? update(g) : g).ToList());

    public static GrowthGoal Goal(
        GrowthGoal g,
        string? title = null,
        string? description = null,
        GrowthGoalStatus? status = null,
        string? category = null,
        bool setCategory = false,
        Priority? priority = null,
        bool setPriority = false,
        string? startDateIso = null,
        bool setStartDate = false,
        string? targetDateIso = null,
        bool setTargetDate = false,
        string? lastUpdatedIso = null,
        int? progressPercent = null,
        bool setProgressPercent = false,
        string? summary = null,
        bool setSummary = false,
        IReadOnlyList<string>? successCriteria = null,
        IReadOnlyList<GrowthGoalAction>? actions = null,
        IReadOnlyList<GrowthGoalCheckIn>? checkIns = null) =>
        new()
        {
            Id = g.Id,
            Title = title ?? g.Title,
            Description = description ?? g.Description,
            Status = status ?? g.Status,
            Category = setCategory ? category : g.Category,
            Priority = setPriority ? priority : g.Priority,
            StartDateIso = setStartDate ? startDateIso : g.StartDateIso,
            TargetDateIso = setTargetDate ? targetDateIso : g.TargetDateIso,
            LastUpdatedIso = lastUpdatedIso ?? g.LastUpdatedIso,
            ProgressPercent = setProgressPercent ? progressPercent : g.ProgressPercent,
            Summary = setSummary ? summary : g.Summary,
            SuccessCriteria = successCriteria ?? g.SuccessCriteria,
            Actions = actions ?? g.Actions,
            CheckIns = checkIns ?? g.CheckIns
        };

    public static GrowthGoalAction Action(
        GrowthGoalAction a,
        string? title = null,
        string? dueDateIso = null,
        bool setDueDate = false,
        GrowthGoalActionState? state = null,
        Priority? priority = null,
        bool setPriority = false,
        string? notes = null,
        bool setNotes = false,
        IReadOnlyList<string>? links = null) =>
        new()
        {
            Id = a.Id,
            Title = title ?? a.Title,
            DueDateIso = setDueDate ? dueDateIso : a.DueDateIso,
            State = state ?? a.State,
            Priority = setPriority ? priority : a.Priority,
            Notes = setNotes ? notes : a.Notes,
            Links = links ?? a.Links
        };

    public static GrowthGoalCheckIn CheckIn(
        GrowthGoalCheckIn c,
        string? dateIso = null,
        GrowthGoalCheckInSignal? signal = null,
        string? note = null) =>
        new()
        {
            Id = c.Id,
            DateIso = dateIso ?? c.DateIso,
            Signal = signal ?? c.Signal,
            Note = note ?? c.Note
        };
}
