using System.Text;
using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Ai.Context;

public sealed class TeamPromptContextBuilder : IAiPromptContextBuilder
{
    private readonly ITeamMemberRepository _teamMembers;
    private readonly IGrowthRepository _growth;

    public TeamPromptContextBuilder(ITeamMemberRepository teamMembers, IGrowthRepository growth)
    {
        _teamMembers = teamMembers;
        _growth = growth;
    }

    public AiViewScope Scope => AiViewScope.Team;

    public async Task<string> BuildContextAsync(AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<TeamMember> members = await _teamMembers.ListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Team context:");
        sb.AppendLine($"- Total team members: {members.Count}");

        if (request.TeamMemberId.HasValue)
        {
            TeamMember? selected = await _teamMembers.GetByIdWithDetailsAsync(request.TeamMemberId.Value, cancellationToken);
            if (selected is not null)
            {
                sb.AppendLine("- Selected team member:");
                sb.AppendLine($"  - Name: {selected.Name}");
                sb.AppendLine($"  - Role: {selected.Role}");
                sb.AppendLine($"  - Status: {selected.StatusDot}");
                if (!string.IsNullOrWhiteSpace(selected.CurrentFocus))
                {
                    sb.AppendLine($"  - Current focus: {Truncate(selected.CurrentFocus, 200)}");
                }

                sb.AppendLine($"  - Signals: load={selected.Signals.Load} | delivery={selected.Signals.Delivery} | support={selected.Signals.SupportNeeded}");

                var recentNotes = selected.Notes
                    .OrderByDescending(n => n.LastModifiedAt ?? n.CreatedAt)
                    .Take(6)
                    .ToList();
                if (recentNotes.Count > 0)
                {
                    sb.AppendLine("  - Recent notes:");
                    foreach (TeamNote note in recentNotes)
                    {
                        var title = string.IsNullOrWhiteSpace(note.Title) ? "(untitled)" : note.Title;
                        sb.AppendLine($"    - {note.Type} | {title} | {Truncate(note.Text, 120)}");
                    }
                }

                if (selected.Risks.Count > 0)
                {
                    sb.AppendLine($"  - Member risks: {selected.Risks.Count}");
                    foreach (TeamMemberRisk risk in selected.Risks.Take(5))
                    {
                        sb.AppendLine($"    - {risk.Title} | {risk.Severity}");
                    }
                }

                Domain.Entities.Growth? growth = await _growth.GetByTeamMemberIdWithDetailsAsync(selected.Id, cancellationToken);
                if (growth is not null)
                {
                    sb.AppendLine($"  - Growth goals: {growth.Goals.Count}; skills in progress: {growth.SkillsInProgress.Count}");
                    foreach (GrowthGoal goal in growth.Goals.Take(5))
                    {
                        sb.AppendLine($"    - {goal.Title} | {goal.Status}");
                    }

                    if (!string.IsNullOrWhiteSpace(growth.FocusAreasMarkdown))
                    {
                        sb.AppendLine($"  - Focus areas: {Truncate(growth.FocusAreasMarkdown, 160)}");
                    }
                }
            }
        }

        var snapshot = members
            .OrderBy(m => m.Name)
            .Take(12)
            .ToList();

        if (snapshot.Count > 0)
        {
            sb.AppendLine("- Team member snapshot:");
            foreach (TeamMember member in snapshot)
            {
                sb.AppendLine($"  - {member.Name} | {member.Role} | status={member.StatusDot} | load={member.Signals.Load} | delivery={member.Signals.Delivery}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string Truncate(string value, int max)
    {
        var trimmed = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (trimmed.Length <= max)
        {
            return trimmed;
        }

        return trimmed[..(max - 1)] + "…";
    }
}
