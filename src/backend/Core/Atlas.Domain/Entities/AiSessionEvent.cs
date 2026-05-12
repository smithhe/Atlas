using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class AiSessionEvent : Entity
{
    public Guid AiSessionId { get; set; }
    public AiSession? AiSession { get; set; }

    public int Sequence { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public string? Delta { get; set; }
    public bool IsTerminal { get; set; }

    public string? ToolName { get; set; }
    public string? ToolCallId { get; set; }
    public string? ArgumentsJson { get; set; }
    public string? ResultJson { get; set; }
}
