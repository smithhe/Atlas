namespace Atlas.Domain.Entities;

/// <summary>
/// Join entity for the Project &lt;-&gt; TeamMember many-to-many relationship.
/// </summary>
public sealed class ProjectTeamMember
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid TeamMemberId { get; set; }
    public TeamMember? TeamMember { get; set; }
}

