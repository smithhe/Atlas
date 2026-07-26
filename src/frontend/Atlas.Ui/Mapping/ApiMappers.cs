using Atlas.Ui.Api.Generated;
using Atlas.Ui.Models;

namespace Atlas.Ui.Mapping;

/// <summary>
/// Thin stubs mirroring React <c>app/api/mappers.ts</c>. Full mapping lands in later phases.
/// </summary>
public static class ApiMappers
{
    public static Settings MapSettings(AtlasApiDTOsSettingsSettingsDto dto)
    {
        // TODO(Phase 5+): merge localStorage defaultAiPanelOpen like React mapSettings.
        return new Settings
        {
            StaleDays = dto.StaleDays ?? 10,
            DefaultAiManualOnly = dto.DefaultAiManualOnly ?? true,
            DefaultAiPanelOpen = false,
            Theme = dto.Theme?.ToString() ?? "Dark",
            AzureDevOpsBaseUrl = dto.AzureDevOpsBaseUrl
        };
    }

    public static ProductOwner MapProductOwner(AtlasApiDTOsProductOwnersProductOwnerListItemDto dto)
    {
        return new ProductOwner
        {
            Id = dto.Id ?? Guid.Empty,
            Name = dto.Name ?? ""
        };
    }

    public static AtlasTask MapTask(AtlasApiDTOsTasksTaskDto dto, TaskLookups lookups)
    {
        // TODO(Phase 5): map status labels, project/risk name lookups like React mapTask.
        string? project = null;
        string? risk = null;
        if (dto.ProjectId is Guid projectId)
        {
            lookups.ProjectNameById.TryGetValue(projectId, out project);
        }

        if (dto.RiskId is Guid riskId)
        {
            lookups.RiskTitleById.TryGetValue(riskId, out risk);
        }

        return new AtlasTask
        {
            Id = dto.Id ?? Guid.Empty,
            Title = dto.Title ?? "",
            Priority = MapPriority(dto.Priority),
            Status = MapTaskStatus(dto.Status),
            AssigneeId = dto.AssigneeId,
            Project = project,
            Risk = risk,
            DueDate = dto.DueDate?.ToString("yyyy-MM-dd"),
            DependencyTaskIds = dto.DependencyTaskIds?.ToList() ?? [],
            EstimatedDurationText = dto.EstimatedDurationText ?? "",
            EstimateConfidence = MapConfidence(dto.EstimateConfidence),
            ActualDurationText = dto.ActualDurationText,
            Notes = dto.Notes ?? "",
            LastTouchedIso = dto.LastTouchedAt?.ToString("o") ?? ""
        };
    }

    public static Project MapProject(AtlasApiDTOsProjectsProjectDto dto)
    {
        // TODO(Phase 5): full project DTO → domain mapping.
        return new Project
        {
            Id = dto.Id ?? Guid.Empty,
            Name = dto.Name ?? "",
            Summary = dto.Summary ?? "",
            Description = dto.Description,
            LinkedTaskIds = dto.LinkedTaskIds?.ToList() ?? [],
            LinkedRiskIds = dto.LinkedRiskIds?.ToList() ?? [],
            TeamMemberIds = dto.TeamMemberIds?.ToList() ?? []
        };
    }

    public static Risk MapRisk(AtlasApiDTOsRisksRiskDto dto, RiskLookups lookups)
    {
        // TODO(Phase 5): history, severity, project name lookup.
        string? project = null;
        if (dto.ProjectId is Guid projectId)
        {
            lookups.ProjectNameById.TryGetValue(projectId, out project);
        }

        return new Risk
        {
            Id = dto.Id ?? Guid.Empty,
            Title = dto.Title ?? "",
            Project = project,
            Description = dto.Description ?? "",
            Evidence = dto.Evidence ?? "",
            LinkedTaskIds = dto.LinkedTaskIds?.ToList() ?? [],
            LinkedTeamMemberIds = dto.LinkedTeamMemberIds?.ToList() ?? [],
            LastUpdatedIso = dto.LastUpdatedAt?.ToString("o") ?? ""
        };
    }

    public static TeamMemberMapResult MapTeamMember(AtlasApiDTOsTeamMembersTeamMemberDto dto)
    {
        // TODO(Phase 6): notes, pins, azure items, member risks, deriveActivitySnapshot.
        var member = new TeamMember
        {
            Id = dto.Id ?? Guid.Empty,
            Name = dto.Name ?? "",
            Role = string.IsNullOrWhiteSpace(dto.Role) ? null : dto.Role,
            StatusDot = dto.StatusDot?.ToString() ?? "Green",
            CurrentFocus = dto.CurrentFocus ?? ""
        };

        return new TeamMemberMapResult
        {
            Member = member,
            MemberRisks = Array.Empty<TeamMemberRisk>()
        };
    }

    public static Growth MapGrowth(AtlasApiDTOsGrowthGrowthDto dto)
    {
        // TODO(Phase 6): goals, skills, feedback themes.
        return new Growth
        {
            Id = dto.Id ?? Guid.Empty,
            MemberId = dto.TeamMemberId ?? Guid.Empty,
            FocusAreasMarkdown = dto.FocusAreasMarkdown ?? ""
        };
    }

    static Priority MapPriority(AtlasDomainEnumsPriority? value) => value switch
    {
        AtlasDomainEnumsPriority.Low => Priority.Low,
        AtlasDomainEnumsPriority.Medium => Priority.Medium,
        AtlasDomainEnumsPriority.High => Priority.High,
        AtlasDomainEnumsPriority.Critical => Priority.Critical,
        _ => Priority.Low
    };

    static Confidence MapConfidence(AtlasDomainEnumsConfidence? value) => value switch
    {
        AtlasDomainEnumsConfidence.Low => Confidence.Low,
        AtlasDomainEnumsConfidence.Medium => Confidence.Medium,
        AtlasDomainEnumsConfidence.High => Confidence.High,
        _ => Confidence.Low
    };

    static Models.TaskStatus? MapTaskStatus(AtlasDomainEnumsTaskStatus? value) => value switch
    {
        AtlasDomainEnumsTaskStatus.NotStarted => Models.TaskStatus.NotStarted,
        AtlasDomainEnumsTaskStatus.InProgress => Models.TaskStatus.InProgress,
        AtlasDomainEnumsTaskStatus.Blocked => Models.TaskStatus.Blocked,
        AtlasDomainEnumsTaskStatus.Done => Models.TaskStatus.Done,
        _ => null
    };
}
