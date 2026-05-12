using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class AiSession : AggregateRoot
{
    public string Title { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string View { get; set; } = string.Empty;
    public string? ActionId { get; set; }

    public Guid? TaskId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? RiskId { get; set; }
    public Guid? TeamMemberId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Status { get; set; } = "started";
    public bool IsTerminal { get; set; }

    public List<AiSessionEvent> Events { get; set; } = [];
}
