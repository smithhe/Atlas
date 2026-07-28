using Atlas.Ui.Api.Generated;
using Atlas.Ui.Models;

namespace Atlas.Ui.Mapping;

/// <summary>DTO → domain mapping mirroring React <c>app/api/mappers.ts</c>.</summary>
public static class ApiMappers
{
    public static Settings MapSettings(AtlasApiDTOsSettingsSettingsDto dto, bool defaultAiPanelOpen = false)
    {
        return new Settings
        {
            StaleDays = dto.StaleDays ?? 10,
            DefaultAiManualOnly = dto.DefaultAiManualOnly ?? true,
            DefaultAiPanelOpen = defaultAiPanelOpen,
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
        return new Project
        {
            Id = dto.Id ?? Guid.Empty,
            Name = dto.Name ?? "",
            Summary = dto.Summary ?? "",
            Description = dto.Description,
            Status = MapProjectStatus(dto.Status),
            Health = MapHealth(dto.Health),
            TargetDateIso = dto.TargetDate?.ToString("yyyy-MM-dd"),
            Priority = dto.Priority is null ? null : MapPriority(dto.Priority),
            ProductOwnerId = dto.ProductOwnerId,
            Tags = dto.Tags?.Select(t => t.Value ?? "").Where(v => v.Length > 0).ToList() ?? [],
            Links = dto.Links?.Select(l => new ProjectLink { Label = l.Label ?? "", Url = l.Url ?? "" }).ToList()
                ?? [],
            LastUpdatedIso = dto.LastUpdatedAt?.ToString("o"),
            LinkedTaskIds = dto.LinkedTaskIds?.ToList() ?? [],
            LinkedRiskIds = dto.LinkedRiskIds?.ToList() ?? [],
            TeamMemberIds = dto.TeamMemberIds?.ToList() ?? []
        };
    }

    public static Risk MapRisk(AtlasApiDTOsRisksRiskDto dto, RiskLookups lookups)
    {
        string? project = null;
        if (dto.ProjectId is Guid projectId)
        {
            lookups.ProjectNameById.TryGetValue(projectId, out project);
        }

        return new Risk
        {
            Id = dto.Id ?? Guid.Empty,
            Title = dto.Title ?? "",
            Status = MapRiskStatus(dto.Status),
            Severity = MapSeverity(dto.Severity),
            Project = project,
            Description = dto.Description ?? "",
            Evidence = dto.Evidence ?? "",
            LinkedTaskIds = dto.LinkedTaskIds?.ToList() ?? [],
            LinkedTeamMemberIds = dto.LinkedTeamMemberIds?.ToList() ?? [],
            History = (dto.History ?? [])
                .Select(h => new RiskHistoryEntry
                {
                    Id = h.Id ?? Guid.Empty,
                    CreatedIso = h.CreatedAt?.ToString("o") ?? "",
                    Text = h.Text ?? ""
                })
                .ToList(),
            LastUpdatedIso = dto.LastUpdatedAt?.ToString("o") ?? ""
        };
    }

    public static TeamMemberMapResult MapTeamMember(AtlasApiDTOsTeamMembersTeamMemberDto dto)
    {
        var notes = (dto.Notes ?? [])
            .Select(n => new TeamNote
            {
                Id = n.Id ?? Guid.Empty,
                CreatedIso = n.CreatedAt?.ToString("o") ?? "",
                LastModifiedIso = n.LastModifiedAt?.ToString("o"),
                Tag = MapNoteTag(n.Type),
                Title = string.IsNullOrWhiteSpace(n.Title) ? null : n.Title,
                Text = n.Text ?? "",
                AdoWorkItemId = string.IsNullOrWhiteSpace(n.AdoWorkItemId) ? null : n.AdoWorkItemId,
                PrUrl = string.IsNullOrWhiteSpace(n.PrUrl) ? null : n.PrUrl
            })
            .ToList();

        var pinnedNoteIds = (dto.Notes ?? [])
            .Where(n => n.PinnedOrder is not null)
            .OrderBy(n => n.PinnedOrder)
            .Select(n => n.Id ?? Guid.Empty)
            .ToList();

        var azureItems = (dto.AzureWorkItems ?? [])
            .Select(w => new AzureItem
            {
                Id = w.Id ?? "",
                Title = w.Title ?? "",
                Status = w.Status ?? "",
                AssignedTo = w.AssignedTo,
                TicketUrl = w.TicketUrl,
                ProjectId = w.ProjectId?.ToString(),
                ChangedDateUtc = w.ChangedDateUtc?.ToString("o"),
                LocalNotes = (w.LocalNotes ?? [])
                    .Select(n => new WorkItemNote
                    {
                        Id = n.Id ?? Guid.Empty,
                        CreatedIso = n.CreatedAt?.ToString("o") ?? "",
                        Text = n.Text ?? ""
                    })
                    .ToList()
            })
            .ToList();

        var member = new TeamMember
        {
            Id = dto.Id ?? Guid.Empty,
            Name = dto.Name ?? "",
            Role = string.IsNullOrWhiteSpace(dto.Role) ? null : dto.Role,
            StatusDot = dto.StatusDot?.ToString() ?? "Green",
            CurrentFocus = dto.CurrentFocus ?? "",
            Profile = new TeamMemberProfile
            {
                TimeZone = dto.Profile?.TimeZone,
                TypicalHours = dto.Profile?.TypicalHours
            },
            Signals = new TeamMemberSignals
            {
                Load = MapLoad(dto.Signals?.Load),
                Delivery = MapDelivery(dto.Signals?.Delivery),
                SupportNeeded = MapSupport(dto.Signals?.SupportNeeded)
            },
            Notes = notes,
            PinnedNoteIds = pinnedNoteIds,
            ActivitySnapshot = new ActivitySnapshot(),
            AzureItems = azureItems
        };

        var memberRisks = (dto.Risks ?? [])
            .Select(r => new TeamMemberRisk
            {
                Id = r.Id ?? Guid.Empty,
                MemberId = member.Id,
                Title = r.Title ?? "",
                Severity = r.Severity?.ToString() ?? "Low",
                RiskType = r.RiskType ?? "",
                Status = r.Status?.ToString() ?? "Open",
                Trend = r.Trend?.ToString() ?? "Stable",
                FirstNoticedDateIso = r.FirstNoticedDate?.ToString("yyyy-MM-dd") ?? "",
                ImpactArea = r.ImpactArea ?? "",
                Description = r.Description ?? "",
                CurrentAction = r.CurrentAction ?? "",
                LastReviewedIso = r.LastReviewedAt?.ToString("o"),
                LinkedRiskId = r.LinkedGlobalRiskId
            })
            .ToList();

        return new TeamMemberMapResult
        {
            Member = member,
            MemberRisks = memberRisks
        };
    }

    public static Growth MapGrowth(AtlasApiDTOsGrowthGrowthDto dto)
    {
        return new Growth
        {
            Id = dto.Id ?? Guid.Empty,
            MemberId = dto.TeamMemberId ?? Guid.Empty,
            FocusAreasMarkdown = dto.FocusAreasMarkdown ?? "",
            SkillsInProgress = dto.SkillsInProgress?.ToList() ?? [],
            Goals = (dto.Goals ?? [])
                .Select(g => new GrowthGoal
                {
                    Id = g.Id ?? Guid.Empty,
                    Title = g.Title ?? "",
                    Description = g.Description ?? "",
                    Status = MapGrowthGoalStatus(g.Status),
                    Category = string.IsNullOrWhiteSpace(g.Category) ? null : g.Category,
                    Priority = g.Priority is null ? null : MapPriority(g.Priority),
                    StartDateIso = g.StartDate?.ToString("yyyy-MM-dd"),
                    TargetDateIso = g.TargetDate?.ToString("yyyy-MM-dd"),
                    LastUpdatedIso = g.LastUpdatedAt?.ToString("o"),
                    ProgressPercent = g.ProgressPercent,
                    Summary = string.IsNullOrWhiteSpace(g.Summary) ? null : g.Summary,
                    SuccessCriteria = g.SuccessCriteria?.ToList() ?? [],
                    Actions = (g.Actions ?? [])
                        .Select(a => new GrowthGoalAction
                        {
                            Id = a.Id ?? Guid.Empty,
                            Title = a.Title ?? "",
                            DueDateIso = a.DueDate?.ToString("yyyy-MM-dd"),
                            State = MapGrowthGoalActionState(a.State),
                            Priority = a.Priority is null ? null : MapPriority(a.Priority),
                            Notes = string.IsNullOrWhiteSpace(a.Notes) ? null : a.Notes,
                            Links = string.IsNullOrWhiteSpace(a.Evidence)
                                ? Array.Empty<string>()
                                : a.Evidence.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .ToList()
                        })
                        .ToList(),
                    CheckIns = (g.CheckIns ?? [])
                        .Select(c => new GrowthGoalCheckIn
                        {
                            Id = c.Id ?? Guid.Empty,
                            DateIso = c.Date?.ToString("yyyy-MM-dd") ?? "",
                            Signal = MapGrowthGoalCheckInSignal(c.Signal),
                            Note = c.Note ?? ""
                        })
                        .ToList()
                })
                .ToList(),
            FeedbackThemes = (dto.FeedbackThemes ?? [])
                .Select(t => new GrowthFeedbackTheme
                {
                    Id = t.Id ?? Guid.Empty,
                    Title = t.Title ?? "",
                    Description = t.Description ?? "",
                    ObservedSinceLabel = string.IsNullOrWhiteSpace(t.ObservedSinceLabel) ? null : t.ObservedSinceLabel
                })
                .ToList()
        };
    }

    public static AtlasDomainEnumsTaskStatus ToApiTaskStatus(Models.TaskStatus? status) => status switch
    {
        Models.TaskStatus.InProgress => AtlasDomainEnumsTaskStatus.InProgress,
        Models.TaskStatus.Blocked => AtlasDomainEnumsTaskStatus.Blocked,
        Models.TaskStatus.Done => AtlasDomainEnumsTaskStatus.Done,
        _ => AtlasDomainEnumsTaskStatus.NotStarted
    };

    public static AtlasDomainEnumsPriority ToApiPriority(Priority priority) => priority switch
    {
        Priority.Medium => AtlasDomainEnumsPriority.Medium,
        Priority.High => AtlasDomainEnumsPriority.High,
        Priority.Critical => AtlasDomainEnumsPriority.Critical,
        _ => AtlasDomainEnumsPriority.Low
    };

    public static AtlasDomainEnumsConfidence ToApiConfidence(Confidence confidence) => confidence switch
    {
        Confidence.Medium => AtlasDomainEnumsConfidence.Medium,
        Confidence.High => AtlasDomainEnumsConfidence.High,
        _ => AtlasDomainEnumsConfidence.Low
    };

    public static AtlasDomainEnumsRiskStatus ToApiRiskStatus(RiskStatus status) => status switch
    {
        RiskStatus.Watching => AtlasDomainEnumsRiskStatus.Watching,
        RiskStatus.Resolved => AtlasDomainEnumsRiskStatus.Resolved,
        _ => AtlasDomainEnumsRiskStatus.Open
    };

    public static AtlasDomainEnumsSeverityLevel ToApiSeverity(string severity) => severity switch
    {
        "Medium" => AtlasDomainEnumsSeverityLevel.Medium,
        "High" => AtlasDomainEnumsSeverityLevel.High,
        _ => AtlasDomainEnumsSeverityLevel.Low
    };

    public static AtlasDomainEnumsProjectStatus ToApiProjectStatus(ProjectStatus? status) => status switch
    {
        ProjectStatus.Paused => AtlasDomainEnumsProjectStatus.Paused,
        ProjectStatus.Completed => AtlasDomainEnumsProjectStatus.Completed,
        _ => AtlasDomainEnumsProjectStatus.Active
    };

    public static AtlasDomainEnumsHealthSignal ToApiHealth(HealthSignal? health) => health switch
    {
        HealthSignal.Yellow => AtlasDomainEnumsHealthSignal.Yellow,
        HealthSignal.Red => AtlasDomainEnumsHealthSignal.Red,
        _ => AtlasDomainEnumsHealthSignal.Green
    };

    public static AtlasDomainEnumsNoteType ToApiNoteType(NoteTag tag) => tag switch
    {
        NoteTag.Blocker => AtlasDomainEnumsNoteType.Blocker,
        NoteTag.Progress => AtlasDomainEnumsNoteType.Progress,
        NoteTag.Concern => AtlasDomainEnumsNoteType.Concern,
        NoteTag.Praise => AtlasDomainEnumsNoteType.Praise,
        NoteTag.Standup => AtlasDomainEnumsNoteType.Standup,
        _ => AtlasDomainEnumsNoteType.Quick
    };

    public static AtlasDomainEnumsStatusDot ToApiStatusDot(string? statusDot) => statusDot?.ToLowerInvariant() switch
    {
        "yellow" => AtlasDomainEnumsStatusDot.Yellow,
        "red" => AtlasDomainEnumsStatusDot.Red,
        _ => AtlasDomainEnumsStatusDot.Green
    };

    public static AtlasDomainEnumsLoadSignal ToApiLoad(LoadSignal load) => load switch
    {
        LoadSignal.Light => AtlasDomainEnumsLoadSignal.Light,
        LoadSignal.Heavy => AtlasDomainEnumsLoadSignal.Heavy,
        _ => AtlasDomainEnumsLoadSignal.Normal
    };

    public static AtlasDomainEnumsDeliverySignal ToApiDelivery(DeliverySignal delivery) => delivery switch
    {
        DeliverySignal.AtRisk => AtlasDomainEnumsDeliverySignal.AtRisk,
        DeliverySignal.Blocked => AtlasDomainEnumsDeliverySignal.Blocked,
        _ => AtlasDomainEnumsDeliverySignal.OnTrack
    };

    public static AtlasDomainEnumsSupportNeededSignal ToApiSupport(SupportNeededSignal support) => support switch
    {
        SupportNeededSignal.Medium => AtlasDomainEnumsSupportNeededSignal.Medium,
        SupportNeededSignal.High => AtlasDomainEnumsSupportNeededSignal.High,
        _ => AtlasDomainEnumsSupportNeededSignal.Low
    };

    public static AtlasDomainEnumsTeamMemberRiskSeverity ToApiTeamMemberRiskSeverity(string severity) => severity switch
    {
        "Medium" => AtlasDomainEnumsTeamMemberRiskSeverity.Medium,
        "High" => AtlasDomainEnumsTeamMemberRiskSeverity.High,
        _ => AtlasDomainEnumsTeamMemberRiskSeverity.Low
    };

    public static AtlasDomainEnumsTeamMemberRiskStatus ToApiTeamMemberRiskStatus(string status) => status switch
    {
        "Mitigating" => AtlasDomainEnumsTeamMemberRiskStatus.Mitigating,
        "Resolved" => AtlasDomainEnumsTeamMemberRiskStatus.Resolved,
        _ => AtlasDomainEnumsTeamMemberRiskStatus.Open
    };

    public static AtlasDomainEnumsTeamMemberRiskTrend ToApiTeamMemberRiskTrend(string trend) => trend switch
    {
        "Improving" => AtlasDomainEnumsTeamMemberRiskTrend.Improving,
        "Worsening" => AtlasDomainEnumsTeamMemberRiskTrend.Worsening,
        _ => AtlasDomainEnumsTeamMemberRiskTrend.Stable
    };

    public static AtlasDomainEnumsGrowthGoalStatus ToApiGrowthGoalStatus(GrowthGoalStatus status) => status switch
    {
        GrowthGoalStatus.NeedsAttention => AtlasDomainEnumsGrowthGoalStatus.NeedsAttention,
        GrowthGoalStatus.Completed => AtlasDomainEnumsGrowthGoalStatus.Completed,
        _ => AtlasDomainEnumsGrowthGoalStatus.OnTrack
    };

    public static AtlasDomainEnumsGrowthGoalActionState ToApiGrowthGoalActionState(GrowthGoalActionState state) => state switch
    {
        GrowthGoalActionState.InProgress => AtlasDomainEnumsGrowthGoalActionState.InProgress,
        GrowthGoalActionState.Complete => AtlasDomainEnumsGrowthGoalActionState.Complete,
        _ => AtlasDomainEnumsGrowthGoalActionState.Planned
    };

    public static AtlasDomainEnumsGrowthGoalCheckInSignal ToApiGrowthGoalCheckInSignal(GrowthGoalCheckInSignal signal) =>
        signal switch
        {
            GrowthGoalCheckInSignal.Mixed => AtlasDomainEnumsGrowthGoalCheckInSignal.Mixed,
            GrowthGoalCheckInSignal.Concern => AtlasDomainEnumsGrowthGoalCheckInSignal.Concern,
            _ => AtlasDomainEnumsGrowthGoalCheckInSignal.Positive
        };

    public static AtlasDomainEnumsTheme ToApiTheme(string? theme) =>
        string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase)
            ? AtlasDomainEnumsTheme.Light
            : AtlasDomainEnumsTheme.Dark;

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

    static RiskStatus MapRiskStatus(AtlasDomainEnumsRiskStatus? value) => value switch
    {
        AtlasDomainEnumsRiskStatus.Open => RiskStatus.Open,
        AtlasDomainEnumsRiskStatus.Watching => RiskStatus.Watching,
        AtlasDomainEnumsRiskStatus.Resolved => RiskStatus.Resolved,
        _ => RiskStatus.Open
    };

    static string MapSeverity(AtlasDomainEnumsSeverityLevel? value) => value switch
    {
        AtlasDomainEnumsSeverityLevel.Low => "Low",
        AtlasDomainEnumsSeverityLevel.Medium => "Medium",
        AtlasDomainEnumsSeverityLevel.High => "High",
        _ => "Low"
    };

    static ProjectStatus? MapProjectStatus(AtlasDomainEnumsProjectStatus? value) => value switch
    {
        AtlasDomainEnumsProjectStatus.Active => ProjectStatus.Active,
        AtlasDomainEnumsProjectStatus.Paused => ProjectStatus.Paused,
        AtlasDomainEnumsProjectStatus.Completed => ProjectStatus.Completed,
        _ => null
    };

    static HealthSignal? MapHealth(AtlasDomainEnumsHealthSignal? value) => value switch
    {
        AtlasDomainEnumsHealthSignal.Green => HealthSignal.Green,
        AtlasDomainEnumsHealthSignal.Yellow => HealthSignal.Yellow,
        AtlasDomainEnumsHealthSignal.Red => HealthSignal.Red,
        _ => null
    };

    static NoteTag MapNoteTag(AtlasDomainEnumsNoteType? value) => value switch
    {
        AtlasDomainEnumsNoteType.Blocker => NoteTag.Blocker,
        AtlasDomainEnumsNoteType.Progress => NoteTag.Progress,
        AtlasDomainEnumsNoteType.Concern => NoteTag.Concern,
        AtlasDomainEnumsNoteType.Praise => NoteTag.Praise,
        AtlasDomainEnumsNoteType.Standup => NoteTag.Standup,
        _ => NoteTag.Quick
    };

    static LoadSignal MapLoad(AtlasDomainEnumsLoadSignal? value) => value switch
    {
        AtlasDomainEnumsLoadSignal.Light => LoadSignal.Light,
        AtlasDomainEnumsLoadSignal.Heavy => LoadSignal.Heavy,
        _ => LoadSignal.Normal
    };

    static DeliverySignal MapDelivery(AtlasDomainEnumsDeliverySignal? value) => value switch
    {
        AtlasDomainEnumsDeliverySignal.AtRisk => DeliverySignal.AtRisk,
        AtlasDomainEnumsDeliverySignal.Blocked => DeliverySignal.Blocked,
        _ => DeliverySignal.OnTrack
    };

    static SupportNeededSignal MapSupport(AtlasDomainEnumsSupportNeededSignal? value) => value switch
    {
        AtlasDomainEnumsSupportNeededSignal.Medium => SupportNeededSignal.Medium,
        AtlasDomainEnumsSupportNeededSignal.High => SupportNeededSignal.High,
        _ => SupportNeededSignal.Low
    };

    static GrowthGoalStatus MapGrowthGoalStatus(AtlasDomainEnumsGrowthGoalStatus? value) => value switch
    {
        AtlasDomainEnumsGrowthGoalStatus.NeedsAttention => GrowthGoalStatus.NeedsAttention,
        AtlasDomainEnumsGrowthGoalStatus.Completed => GrowthGoalStatus.Completed,
        _ => GrowthGoalStatus.OnTrack
    };

    static GrowthGoalActionState MapGrowthGoalActionState(AtlasDomainEnumsGrowthGoalActionState? value) => value switch
    {
        AtlasDomainEnumsGrowthGoalActionState.InProgress => GrowthGoalActionState.InProgress,
        AtlasDomainEnumsGrowthGoalActionState.Complete => GrowthGoalActionState.Complete,
        _ => GrowthGoalActionState.Planned
    };

    static GrowthGoalCheckInSignal MapGrowthGoalCheckInSignal(AtlasDomainEnumsGrowthGoalCheckInSignal? value) =>
        value switch
        {
            AtlasDomainEnumsGrowthGoalCheckInSignal.Mixed => GrowthGoalCheckInSignal.Mixed,
            AtlasDomainEnumsGrowthGoalCheckInSignal.Concern => GrowthGoalCheckInSignal.Concern,
            _ => GrowthGoalCheckInSignal.Positive
        };
}
