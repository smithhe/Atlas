namespace Atlas.Domain.Entities;

/// <summary>
/// Join entity for the Risk &lt;-&gt; TeamMember many-to-many relationship.
/// </summary>
public sealed class RiskTeamMember
{
    public Guid RiskId { get; set; }
    public Risk? Risk { get; set; }

    public Guid TeamMemberId { get; set; }
    public TeamMember? TeamMember { get; set; }
}

