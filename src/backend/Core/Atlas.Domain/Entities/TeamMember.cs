using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;
using Atlas.Domain.ValueObjects;

namespace Atlas.Domain.Entities;

public sealed class TeamMember : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public StatusDot StatusDot { get; set; }
    public string CurrentFocus { get; set; } = string.Empty;

    public TeamMemberProfile Profile { get; set; } = new();
    public TeamMemberSignals Signals { get; set; } = new();

    public List<TeamNote> Notes { get; set; } = [];
    public List<TeamMemberRisk> Risks { get; set; } = [];

    /// <summary>
    /// Project membership for this team member (explicit many-to-many join).
    /// </summary>
    public List<ProjectTeamMember> Projects { get; set; } = [];

    /// <summary>
    /// Links from global risks to this member (explicit many-to-many join).
    /// </summary>
    public List<RiskTeamMember> LinkedRisks { get; set; } = [];

    /// <summary>
    /// Linked Azure work items assigned to this member.
    /// </summary>
    public List<AzureWorkItemLink> AzureWorkItemLinks { get; set; } = [];
}

