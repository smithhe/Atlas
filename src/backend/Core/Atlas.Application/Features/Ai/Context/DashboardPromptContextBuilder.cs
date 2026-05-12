using System.Text;
using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Ai.Context;

public sealed class DashboardPromptContextBuilder : IAiPromptContextBuilder
{
    private readonly ITaskRepository _tasks;
    private readonly IRiskRepository _risks;
    private readonly ITeamMemberRepository _teamMembers;
    private readonly IProjectRepository _projects;

    public DashboardPromptContextBuilder(
        ITaskRepository tasks,
        IRiskRepository risks,
        ITeamMemberRepository teamMembers,
        IProjectRepository projects)
    {
        _tasks = tasks;
        _risks = risks;
        _teamMembers = teamMembers;
        _projects = projects;
    }

    public AiViewScope Scope => AiViewScope.Dashboard;

    public async Task<string> BuildContextAsync(AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.TaskItem> tasks = await _tasks.ListAsync(cancellationToken: cancellationToken);
        IReadOnlyList<Domain.Entities.Risk> risks = await _risks.ListAsync(cancellationToken);
        IReadOnlyList<Domain.Entities.TeamMember> teamMembers = await _teamMembers.ListAsync(cancellationToken);
        IReadOnlyList<Domain.Entities.Project> projects = await _projects.ListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Dashboard context:");
        sb.AppendLine($"- Total tasks: {tasks.Count}");
        sb.AppendLine($"- Open/Watching risks: {risks.Count(r => r.Status != Domain.Enums.RiskStatus.Resolved)}");
        sb.AppendLine($"- Team members: {teamMembers.Count}");
        sb.AppendLine($"- Projects: {projects.Count}");

        var urgentTasks = tasks
            .Where(t => t.Status == Domain.Enums.TaskStatus.Blocked || t.Priority is Domain.Enums.Priority.High or Domain.Enums.Priority.Critical)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Title)
            .Take(8)
            .ToList();

        if (urgentTasks.Count > 0)
        {
            sb.AppendLine("- Urgent tasks:");
            foreach (Domain.Entities.TaskItem task in urgentTasks)
            {
                sb.AppendLine($"  - {task.Title} | {task.Priority} | {task.Status}");
            }
        }

        var activeRisks = risks
            .Where(r => r.Status != Domain.Enums.RiskStatus.Resolved)
            .OrderByDescending(r => r.Severity)
            .ThenBy(r => r.Title)
            .Take(6)
            .ToList();

        if (activeRisks.Count > 0)
        {
            sb.AppendLine("- Active risks:");
            foreach (Domain.Entities.Risk risk in activeRisks)
            {
                sb.AppendLine($"  - {risk.Title} | {risk.Severity} | {risk.Status}");
            }
        }

        var teamSignals = teamMembers
            .OrderBy(m => m.Name)
            .Take(8)
            .ToList();

        if (teamSignals.Count > 0)
        {
            sb.AppendLine("- Team signals:");
            foreach (Domain.Entities.TeamMember member in teamSignals)
            {
                sb.AppendLine($"  - {member.Name} | status={member.StatusDot} | load={member.Signals.Load} | delivery={member.Signals.Delivery}");
            }
        }

        return sb.ToString().TrimEnd();
    }
}

