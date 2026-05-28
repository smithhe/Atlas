using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class AiConversation : AggregateRoot
{
    public string Title { get; set; } = string.Empty;
    public string View { get; set; } = string.Empty;
    public string? ActionId { get; set; }

    public Guid? TaskId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? RiskId { get; set; }
    public Guid? TeamMemberId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<AiSession> Turns { get; set; } = [];
}
