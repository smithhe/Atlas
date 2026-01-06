using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Risks;

public sealed record RiskDto(
    Guid Id,
    string Title,
    RiskStatus Status,
    SeverityLevel Severity,
    Guid? ProjectId,
    string Description,
    string Evidence,
    IReadOnlyList<Guid> LinkedTaskIds,
    IReadOnlyList<Guid> LinkedTeamMemberIds,
    IReadOnlyList<RiskHistoryEntryDto> History,
    DateTimeOffset LastUpdatedAt);

