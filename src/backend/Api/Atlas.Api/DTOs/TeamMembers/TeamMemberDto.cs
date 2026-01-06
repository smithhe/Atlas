using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.TeamMembers;

public sealed record TeamMemberProfileDto(string? TimeZone, string? TypicalHours);
public sealed record TeamMemberSignalsDto(LoadSignal Load, DeliverySignal Delivery, SupportNeededSignal SupportNeeded);

public sealed record TeamNoteDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt,
    NoteType Type,
    string? Title,
    string Text,
    int? PinnedOrder);

public sealed record TeamMemberRiskDto(
    Guid Id,
    string Title,
    TeamMemberRiskSeverity Severity,
    string RiskType,
    TeamMemberRiskStatus Status,
    TeamMemberRiskTrend Trend,
    DateOnly FirstNoticedDate,
    string ImpactArea,
    string Description,
    string CurrentAction,
    DateTimeOffset? LastReviewedAt,
    Guid? LinkedGlobalRiskId);

public sealed record TeamMemberListItemDto(
    Guid Id,
    string Name,
    string Role,
    StatusDot StatusDot);

public sealed record TeamMemberDto(
    Guid Id,
    string Name,
    string Role,
    StatusDot StatusDot,
    string CurrentFocus,
    TeamMemberProfileDto Profile,
    TeamMemberSignalsDto Signals,
    IReadOnlyList<TeamNoteDto> Notes,
    IReadOnlyList<TeamMemberRiskDto> Risks,
    IReadOnlyList<Guid> ProjectIds,
    IReadOnlyList<Guid> LinkedGlobalRiskIds);

