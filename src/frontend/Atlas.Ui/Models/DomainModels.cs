namespace Atlas.Ui.Models;

/// <summary>Thin domain shapes mirroring React <c>app/types.ts</c> (Phase 3 stubs).</summary>
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

public enum RiskStatus
{
    Open,
    Watching,
    Resolved
}

public enum Confidence
{
    Low,
    Medium,
    High
}

public enum TaskStatus
{
    NotStarted,
    InProgress,
    Blocked,
    Done
}

public enum HealthSignal
{
    Green,
    Yellow,
    Red
}

public enum ProjectStatus
{
    Active,
    Paused,
    Completed
}

public enum LoadSignal
{
    Light,
    Normal,
    Heavy
}

public enum DeliverySignal
{
    AtRisk,
    OnTrack,
    Blocked
}

public enum SupportNeededSignal
{
    Low,
    Medium,
    High
}

public enum GrowthGoalStatus
{
    OnTrack,
    NeedsAttention,
    Completed
}

public enum GrowthGoalActionState
{
    Planned,
    InProgress,
    Complete
}

public enum GrowthGoalCheckInSignal
{
    Positive,
    Mixed,
    Concern
}

public enum NoteTag
{
    Blocker,
    Progress,
    Concern,
    Praise,
    Standup,
    Quick
}

public sealed class Settings
{
    public int StaleDays { get; set; }
    public bool DefaultAiManualOnly { get; set; }
    public bool DefaultAiPanelOpen { get; set; }
    public string Theme { get; set; } = "Dark";
    public string? AzureDevOpsBaseUrl { get; set; }
}

public sealed class ProductOwner
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class AtlasTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public Priority Priority { get; set; }
    public TaskStatus? Status { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? Project { get; set; }
    public string? Risk { get; set; }
    public string? DueDate { get; set; }
    public IReadOnlyList<Guid> DependencyTaskIds { get; set; } = Array.Empty<Guid>();
    public string EstimatedDurationText { get; set; } = "";
    public Confidence EstimateConfidence { get; set; }
    public string? ActualDurationText { get; set; }
    public string Notes { get; set; } = "";
    public string LastTouchedIso { get; set; } = "";
}

public sealed class ProjectCheckIn
{
    public string DateIso { get; set; } = "";
    public string Note { get; set; } = "";
}

public sealed class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? Description { get; set; }
    public ProjectStatus? Status { get; set; }
    public HealthSignal? Health { get; set; }
    public string? TargetDateIso { get; set; }
    public Priority? Priority { get; set; }
    public Guid? ProductOwnerId { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ProjectLink> Links { get; set; } = Array.Empty<ProjectLink>();
    public string? LastUpdatedIso { get; set; }
    public ProjectCheckIn? LatestCheckIn { get; set; }
    public IReadOnlyList<Guid> LinkedTaskIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> LinkedRiskIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> TeamMemberIds { get; set; } = Array.Empty<Guid>();
}

public sealed class ProjectLink
{
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
}

public sealed class Risk
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public RiskStatus Status { get; set; }
    public string Severity { get; set; } = "Low";
    public string? Project { get; set; }
    public Guid? OwnerId { get; set; }
    public string Description { get; set; } = "";
    public string Evidence { get; set; } = "";
    public IReadOnlyList<Guid> LinkedTaskIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> LinkedTeamMemberIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<RiskHistoryEntry> History { get; set; } = Array.Empty<RiskHistoryEntry>();
    public string LastUpdatedIso { get; set; } = "";
}

public sealed class RiskHistoryEntry
{
    public Guid Id { get; set; }
    public string CreatedIso { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class TeamNote
{
    public Guid Id { get; set; }
    public string CreatedIso { get; set; } = "";
    public string? LastModifiedIso { get; set; }
    public NoteTag Tag { get; set; }
    public string? Title { get; set; }
    public string Text { get; set; } = "";
    public string? AdoWorkItemId { get; set; }
    public string? PrUrl { get; set; }
}

public sealed class AzureItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string? AssignedTo { get; set; }
    public string? TicketUrl { get; set; }
    public string? ProjectId { get; set; }
    public string? ChangedDateUtc { get; set; }
    public string? TimeTaken { get; set; }
    public string? StartDateIso { get; set; }
    public string? CommitsUrl { get; set; }
    public IReadOnlyList<string> PrUrls { get; set; } = Array.Empty<string>();
    public IReadOnlyList<WorkItemNote> LocalNotes { get; set; } = Array.Empty<WorkItemNote>();
}

public sealed class WorkItemNote
{
    public Guid Id { get; set; }
    public string CreatedIso { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class TeamMember
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Role { get; set; }
    public string StatusDot { get; set; } = "Green";
    public string CurrentFocus { get; set; } = "";
    public TeamMemberProfile Profile { get; set; } = new();
    public TeamMemberSignals Signals { get; set; } = new();
    public IReadOnlyList<TeamNote> Notes { get; set; } = Array.Empty<TeamNote>();
    public IReadOnlyList<Guid> PinnedNoteIds { get; set; } = Array.Empty<Guid>();
    public ActivitySnapshot ActivitySnapshot { get; set; } = new();
    public IReadOnlyList<AzureItem> AzureItems { get; set; } = Array.Empty<AzureItem>();
}

public sealed class TeamMemberProfile
{
    public string? TimeZone { get; set; }
    public string? TypicalHours { get; set; }
}

public sealed class TeamMemberSignals
{
    public LoadSignal Load { get; set; }
    public DeliverySignal Delivery { get; set; }
    public SupportNeededSignal SupportNeeded { get; set; }
}

public sealed class ActivitySnapshot
{
    public IReadOnlyList<string> Bullets { get; set; } = Array.Empty<string>();
    public string? LastUpdatedIso { get; set; }
    public IReadOnlyList<string>? QuickTags { get; set; }
}

public sealed class TeamMemberRisk
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string Title { get; set; } = "";
    public string Severity { get; set; } = "Low";
    public string RiskType { get; set; } = "";
    public string Status { get; set; } = "Open";
    public string Trend { get; set; } = "Stable";
    public string FirstNoticedDateIso { get; set; } = "";
    public string ImpactArea { get; set; } = "";
    public string Description { get; set; } = "";
    public string CurrentAction { get; set; } = "";
    public string? LastReviewedIso { get; set; }
    public Guid? LinkedRiskId { get; set; }
}

public enum GrowthLoadStatus
{
    Idle,
    Loading,
    Succeeded,
    Failed
}

public sealed class Growth
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public IReadOnlyList<GrowthGoal> Goals { get; set; } = Array.Empty<GrowthGoal>();
    public IReadOnlyList<string> SkillsInProgress { get; set; } = Array.Empty<string>();
    public IReadOnlyList<GrowthFeedbackTheme> FeedbackThemes { get; set; } = Array.Empty<GrowthFeedbackTheme>();
    public string FocusAreasMarkdown { get; set; } = "";
}

public sealed class GrowthGoal
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public GrowthGoalStatus Status { get; set; }
    public string? Category { get; set; }
    public Priority? Priority { get; set; }
    public string? StartDateIso { get; set; }
    public string? TargetDateIso { get; set; }
    public string? LastUpdatedIso { get; set; }
    public int? ProgressPercent { get; set; }
    public string? Summary { get; set; }
    public IReadOnlyList<string> SuccessCriteria { get; set; } = Array.Empty<string>();
    public IReadOnlyList<GrowthGoalAction> Actions { get; set; } = Array.Empty<GrowthGoalAction>();
    public IReadOnlyList<GrowthGoalCheckIn> CheckIns { get; set; } = Array.Empty<GrowthGoalCheckIn>();
}

public sealed class GrowthGoalAction
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? DueDateIso { get; set; }
    public GrowthGoalActionState State { get; set; }
    public Priority? Priority { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<string> Links { get; set; } = Array.Empty<string>();
}

public sealed class GrowthGoalCheckIn
{
    public Guid Id { get; set; }
    public string DateIso { get; set; } = "";
    public GrowthGoalCheckInSignal Signal { get; set; }
    public string Note { get; set; } = "";
}

public sealed class GrowthFeedbackTheme
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ObservedSinceLabel { get; set; }
}

public sealed class TaskLookups
{
    public IReadOnlyDictionary<Guid, string> ProjectNameById { get; init; } =
        new Dictionary<Guid, string>();

    public IReadOnlyDictionary<Guid, string> RiskTitleById { get; init; } =
        new Dictionary<Guid, string>();
}

public sealed class RiskLookups
{
    public IReadOnlyDictionary<Guid, string> ProjectNameById { get; init; } =
        new Dictionary<Guid, string>();
}

public sealed class TeamMemberMapResult
{
    public required TeamMember Member { get; init; }
    public required IReadOnlyList<TeamMemberRisk> MemberRisks { get; init; }
}
