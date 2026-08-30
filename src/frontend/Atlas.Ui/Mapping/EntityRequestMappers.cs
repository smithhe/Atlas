using Atlas.Ui.Api.Generated;
using Atlas.Ui.Models;

namespace Atlas.Ui.Mapping;

/// <summary>Domain entity → NSwag request DTOs for create/update mutations.</summary>
public static class EntityRequestMappers
{
    public static AtlasApiDTOsTasksCreateTaskRequest ToCreateTaskRequest(
        AtlasTask task,
        IReadOnlyList<Project> projects,
        IReadOnlyList<Risk> risks)
    {
        TaskRequestFields fields = GetTaskRequestFields(task, projects, risks);
        return new()
        {
            Title = fields.Title,
            Priority = fields.Priority,
            Status = fields.Status,
            AssigneeId = fields.AssigneeId,
            ProjectId = fields.ProjectId,
            RiskId = fields.RiskId,
            DueDate = fields.DueDate,
            DependencyTaskIds = fields.DependencyTaskIds,
            EstimatedDurationText = fields.EstimatedDurationText,
            EstimateConfidence = fields.EstimateConfidence,
            ActualDurationText = fields.ActualDurationText,
            Notes = fields.Notes
        };
    }

    public static AtlasApiDTOsTasksUpdateTaskRequest ToUpdateTaskRequest(
        AtlasTask task,
        IReadOnlyList<Project> projects,
        IReadOnlyList<Risk> risks)
    {
        TaskRequestFields fields = GetTaskRequestFields(task, projects, risks);
        return new()
        {
            Title = fields.Title,
            Priority = fields.Priority,
            Status = fields.Status,
            AssigneeId = fields.AssigneeId,
            ProjectId = fields.ProjectId,
            RiskId = fields.RiskId,
            DueDate = fields.DueDate,
            DependencyTaskIds = fields.DependencyTaskIds,
            EstimatedDurationText = fields.EstimatedDurationText,
            EstimateConfidence = fields.EstimateConfidence,
            ActualDurationText = fields.ActualDurationText,
            Notes = fields.Notes
        };
    }

    private static TaskRequestFields GetTaskRequestFields(
        AtlasTask task,
        IReadOnlyList<Project> projects,
        IReadOnlyList<Risk> risks) =>
        new(
            task.Title,
            ApiMappers.ToApiPriority(task.Priority),
            ApiMappers.ToApiTaskStatus(task.Status),
            task.AssigneeId,
            FindProjectIdByName(task.Project, projects),
            FindRiskIdByTitle(task.Risk, risks),
            ParseDate(task.DueDate),
            task.DependencyTaskIds.ToList(),
            task.EstimatedDurationText,
            ApiMappers.ToApiConfidence(task.EstimateConfidence),
            task.ActualDurationText,
            task.Notes);

    private sealed record TaskRequestFields(
        string Title,
        AtlasDomainEnumsPriority Priority,
        AtlasDomainEnumsTaskStatus? Status,
        Guid? AssigneeId,
        Guid? ProjectId,
        Guid? RiskId,
        DateTimeOffset? DueDate,
        List<Guid> DependencyTaskIds,
        string EstimatedDurationText,
        AtlasDomainEnumsConfidence EstimateConfidence,
        string? ActualDurationText,
        string Notes);

    public static AtlasApiDTOsRisksCreateRiskRequest ToCreateRiskRequest(
        Risk risk,
        IReadOnlyList<Project> projects) =>
        new()
        {
            Title = risk.Title,
            Status = ApiMappers.ToApiRiskStatus(risk.Status),
            Severity = ApiMappers.ToApiSeverity(risk.Severity),
            ProjectId = FindProjectIdByName(risk.Project, projects),
            Description = risk.Description,
            Evidence = risk.Evidence
        };

    public static AtlasApiDTOsRisksUpdateRiskRequest ToUpdateRiskRequest(
        Risk risk,
        IReadOnlyList<Project> projects) =>
        new()
        {
            Title = risk.Title,
            Status = ApiMappers.ToApiRiskStatus(risk.Status),
            Severity = ApiMappers.ToApiSeverity(risk.Severity),
            ProjectId = FindProjectIdByName(risk.Project, projects),
            Description = risk.Description,
            Evidence = risk.Evidence
        };

    public static AtlasApiDTOsProjectsCreateProjectRequest ToCreateProjectRequest(Project project) =>
        new()
        {
            Name = project.Name,
            Summary = project.Summary,
            Description = project.Description,
            Status = ApiMappers.ToApiProjectStatus(project.Status),
            Health = ApiMappers.ToApiHealth(project.Health),
            TargetDate = ParseDate(project.TargetDateIso),
            Priority = project.Priority is null ? null : ApiMappers.ToApiPriority(project.Priority.Value),
            ProductOwnerId = project.ProductOwnerId,
            Tags = project.Tags.ToList(),
            Links = project.Links.Select(l => new AtlasApiDTOsProjectsProjectLinkDto { Label = l.Label, Url = l.Url }).ToList()
        };

    public static AtlasApiDTOsProjectsUpdateProjectRequest ToUpdateProjectRequest(Project project) =>
        new()
        {
            Name = project.Name,
            Summary = project.Summary,
            Description = project.Description,
            Status = ApiMappers.ToApiProjectStatus(project.Status),
            Health = ApiMappers.ToApiHealth(project.Health),
            TargetDate = ParseDate(project.TargetDateIso),
            Priority = project.Priority is null ? null : ApiMappers.ToApiPriority(project.Priority.Value),
            ProductOwnerId = project.ProductOwnerId,
            Tags = project.Tags.ToList(),
            Links = project.Links.Select(l => new AtlasApiDTOsProjectsProjectLinkDto { Label = l.Label, Url = l.Url }).ToList()
        };

    public static AtlasApiDTOsSettingsUpdateSettingsRequest ToUpdateSettingsRequest(Settings settings) =>
        new()
        {
            StaleDays = settings.StaleDays,
            DefaultAiManualOnly = settings.DefaultAiManualOnly,
            Theme = ApiMappers.ToApiTheme(settings.Theme),
            AzureDevOpsBaseUrl = string.IsNullOrWhiteSpace(settings.AzureDevOpsBaseUrl) ? null : settings.AzureDevOpsBaseUrl
        };

    public static AtlasApiDTOsTeamMembersUpdateTeamMemberRequest ToUpdateTeamMemberRequest(TeamMember member) =>
        new()
        {
            Name = member.Name,
            Role = member.Role,
            StatusDot = ApiMappers.ToApiStatusDot(member.StatusDot),
            CurrentFocus = member.CurrentFocus
        };

    public static AtlasApiDTOsTeamMembersProfileUpdateTeamMemberProfileRequest ToUpdateTeamMemberProfileRequest(TeamMemberProfile profile) =>
        new()
        {
            TimeZone = profile.TimeZone,
            TypicalHours = profile.TypicalHours
        };

    public static AtlasApiDTOsTeamMembersSignalsUpdateTeamMemberSignalsRequest ToUpdateTeamMemberSignalsRequest(TeamMemberSignals signals) =>
        new()
        {
            Load = ApiMappers.ToApiLoad(signals.Load),
            Delivery = ApiMappers.ToApiDelivery(signals.Delivery),
            SupportNeeded = ApiMappers.ToApiSupport(signals.SupportNeeded)
        };

    public static AtlasApiDTOsTeamMembersRisksAddTeamMemberRiskRequest ToAddTeamMemberRiskRequest(TeamMemberRisk draft) =>
        new()
        {
            Title = draft.Title,
            Severity = ApiMappers.ToApiTeamMemberRiskSeverity(draft.Severity),
            RiskType = draft.RiskType,
            Status = ApiMappers.ToApiTeamMemberRiskStatus(draft.Status),
            Trend = ApiMappers.ToApiTeamMemberRiskTrend(draft.Trend),
            FirstNoticedDate = ParseDate(draft.FirstNoticedDateIso),
            ImpactArea = draft.ImpactArea,
            Description = draft.Description,
            CurrentAction = draft.CurrentAction,
            LinkedGlobalRiskId = draft.LinkedRiskId
        };

    public static AtlasApiDTOsTeamMembersRisksUpdateTeamMemberRiskRequest ToUpdateTeamMemberRiskRequest(TeamMemberRisk risk) =>
        new()
        {
            Title = risk.Title,
            Severity = ApiMappers.ToApiTeamMemberRiskSeverity(risk.Severity),
            RiskType = risk.RiskType,
            Status = ApiMappers.ToApiTeamMemberRiskStatus(risk.Status),
            Trend = ApiMappers.ToApiTeamMemberRiskTrend(risk.Trend),
            FirstNoticedDate = ParseDate(risk.FirstNoticedDateIso),
            ImpactArea = risk.ImpactArea,
            Description = risk.Description,
            CurrentAction = risk.CurrentAction,
            LinkedGlobalRiskId = risk.LinkedRiskId,
            LastReviewedAt = ParseDate(risk.LastReviewedIso)
        };

    public static AtlasApiDTOsTeamMembersAzureWorkItemsAddAzureWorkItemLocalNoteRequest ToAddAzureWorkItemLocalNoteRequest(string text) =>
        new() { Text = text };

    public static AtlasApiDTOsTeamMembersNotesAddTeamNoteRequest ToAddTeamNoteRequest(
        NoteTag tag,
        string text,
        string? title,
        string? adoWorkItemId,
        string? prUrl) =>
        new()
        {
            Type = ApiMappers.ToApiNoteType(tag),
            Title = title,
            Text = text,
            AdoWorkItemId = adoWorkItemId,
            PrUrl = prUrl
        };

    public static AtlasApiDTOsTeamMembersNotesUpdateTeamNoteRequest ToUpdateTeamNoteRequest(TeamNote note) =>
        new()
        {
            Type = ApiMappers.ToApiNoteType(note.Tag),
            Title = note.Title,
            Text = note.Text,
            AdoWorkItemId = note.AdoWorkItemId,
            PrUrl = note.PrUrl
        };

    public static AtlasApiDTOsGrowthGoalsAddGrowthGoalRequest ToAddGrowthGoalRequest(GrowthGoal draft) =>
        new()
        {
            Title = draft.Title,
            Description = draft.Description,
            Status = ApiMappers.ToApiGrowthGoalStatus(draft.Status),
            Category = draft.Category,
            Priority = draft.Priority is null ? null : ApiMappers.ToApiPriority(draft.Priority.Value),
            StartDate = ParseDate(draft.StartDateIso),
            TargetDate = ParseDate(draft.TargetDateIso)
        };

    public static AtlasApiDTOsGrowthGoalsUpdateGrowthGoalRequest ToUpdateGrowthGoalRequest(GrowthGoal goal) =>
        new()
        {
            Title = goal.Title,
            Description = goal.Description,
            Status = ApiMappers.ToApiGrowthGoalStatus(goal.Status),
            StartDate = ParseDate(goal.StartDateIso),
            TargetDate = ParseDate(goal.TargetDateIso),
            Category = goal.Category,
            Priority = goal.Priority is null ? null : ApiMappers.ToApiPriority(goal.Priority.Value),
            ProgressPercent = goal.ProgressPercent,
            Summary = goal.Summary,
            SuccessCriteria = goal.SuccessCriteria.ToList()
        };

    public static AtlasApiDTOsGrowthGoalsActionsAddGrowthGoalActionRequest ToAddGrowthGoalActionRequest(
        GrowthGoalAction draft,
        Priority? goalPriority) =>
        new()
        {
            Title = draft.Title,
            State = ApiMappers.ToApiGrowthGoalActionState(draft.State),
            Priority = draft.Priority is null
                ? goalPriority is null ? AtlasDomainEnumsPriority.Medium : ApiMappers.ToApiPriority(goalPriority.Value)
                : ApiMappers.ToApiPriority(draft.Priority.Value)
        };

    public static AtlasApiDTOsGrowthGoalsActionsUpdateGrowthGoalActionRequest ToUpdateGrowthGoalActionRequest(GrowthGoalAction action) =>
        new()
        {
            Title = action.Title,
            State = ApiMappers.ToApiGrowthGoalActionState(action.State),
            DueDate = ParseDate(action.DueDateIso),
            Priority = action.Priority is null ? null : ApiMappers.ToApiPriority(action.Priority.Value),
            Notes = action.Notes,
            Evidence = action.Links.Count > 0 ? string.Join('\n', action.Links) : null
        };

    public static AtlasApiDTOsGrowthGoalsCheckInsAddGrowthGoalCheckInRequest ToAddGrowthGoalCheckInRequest(GrowthGoalCheckIn draft) =>
        new()
        {
            Date = ParseDate(draft.DateIso) ?? DateTimeOffset.Now,
            Signal = ApiMappers.ToApiGrowthGoalCheckInSignal(draft.Signal),
            Note = draft.Note
        };

    public static AtlasApiDTOsGrowthGoalsCheckInsUpdateGrowthGoalCheckInRequest ToUpdateGrowthGoalCheckInRequest(GrowthGoalCheckIn checkIn) =>
        new()
        {
            Date = ParseDate(checkIn.DateIso),
            Signal = ApiMappers.ToApiGrowthGoalCheckInSignal(checkIn.Signal),
            Note = checkIn.Note.Trim()
        };

    public static AtlasApiDTOsGrowthSetGrowthSkillsInProgressRequest ToSetGrowthSkillsInProgressRequest(IReadOnlyList<string> skills) =>
        new() { SkillsInProgress = skills.ToList() };

    public static AtlasApiDTOsGrowthFeedbackThemesAddFeedbackThemeRequest ToAddFeedbackThemeRequest(GrowthFeedbackTheme draft) =>
        new()
        {
            Title = draft.Title,
            Description = draft.Description,
            ObservedSinceLabel = draft.ObservedSinceLabel
        };

    public static AtlasApiDTOsGrowthFeedbackThemesUpdateFeedbackThemeRequest ToUpdateFeedbackThemeRequest(GrowthFeedbackTheme theme) =>
        new()
        {
            Title = theme.Title,
            Description = theme.Description,
            ObservedSinceLabel = theme.ObservedSinceLabel
        };

    public static AtlasApiDTOsGrowthUpdateGrowthFocusAreasRequest ToUpdateGrowthFocusAreasRequest(string? markdown) =>
        new() { FocusAreasMarkdown = markdown };

    private static Guid? FindProjectIdByName(string? name, IReadOnlyList<Project> projects) =>
        string.IsNullOrWhiteSpace(name) ? null : projects.FirstOrDefault(p => p.Name == name)?.Id;

    private static Guid? FindRiskIdByTitle(string? title, IReadOnlyList<Risk> risks) =>
        string.IsNullOrWhiteSpace(title) ? null : risks.FirstOrDefault(r => r.Title == title)?.Id;

    private static DateTimeOffset? ParseDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
        {
            return null;
        }

        return DateTimeOffset.TryParse(iso, out DateTimeOffset d) ? d : null;
    }
}
