using System.Text;
using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Ai.Context;

public sealed class TasksPromptContextBuilder : IAiPromptContextBuilder
{
    private readonly ITaskRepository _tasks;
    private readonly IRiskRepository _risks;
    private readonly IProjectRepository _projects;
    private readonly ITeamMemberRepository _teamMembers;

    public TasksPromptContextBuilder(
        ITaskRepository tasks,
        IRiskRepository risks,
        IProjectRepository projects,
        ITeamMemberRepository teamMembers)
    {
        _tasks = tasks;
        _risks = risks;
        _projects = projects;
        _teamMembers = teamMembers;
    }

    public AiViewScope Scope => AiViewScope.Tasks;

    public async Task<string> BuildContextAsync(AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.TaskItem> allTasks = await _tasks.ListAsync(cancellationToken: cancellationToken);
        IReadOnlyList<Domain.Entities.Risk> allRisks = await _risks.ListAsync(cancellationToken);
        IReadOnlyList<Domain.Entities.Project> allProjects = await _projects.ListAsync(cancellationToken);
        IReadOnlyList<Domain.Entities.TeamMember> allTeamMembers = await _teamMembers.ListAsync(cancellationToken);

        var projectById = allProjects.ToDictionary(p => p.Id, p => p.Name);
        var riskById = allRisks.ToDictionary(r => r.Id, r => r.Title);
        var memberById = allTeamMembers.ToDictionary(m => m.Id, m => m.Name);

        var selectedTask = request.TaskId.HasValue
            ? allTasks.FirstOrDefault(t => t.Id == request.TaskId.Value)
            : null;

        var sb = new StringBuilder();
        sb.AppendLine("Tasks context:");
        sb.AppendLine($"- Total tasks: {allTasks.Count}");

        if (selectedTask is not null)
        {
            sb.AppendLine("- Selected task:");
            sb.AppendLine($"  - Title: {selectedTask.Title}");
            sb.AppendLine($"  - Priority: {selectedTask.Priority}");
            sb.AppendLine($"  - Status: {selectedTask.Status}");
            sb.AppendLine($"  - Due: {(selectedTask.DueDate.HasValue ? selectedTask.DueDate.Value.ToString("yyyy-MM-dd") : "none")}");
            if (selectedTask.ProjectId.HasValue && projectById.TryGetValue(selectedTask.ProjectId.Value, out string? project))
            {
                sb.AppendLine($"  - Project: {project}");
            }

            if (selectedTask.RiskId.HasValue && riskById.TryGetValue(selectedTask.RiskId.Value, out string? risk))
            {
                sb.AppendLine($"  - Risk: {risk}");
            }

            if (selectedTask.AssigneeId.HasValue && memberById.TryGetValue(selectedTask.AssigneeId.Value, out string? assignee))
            {
                sb.AppendLine($"  - Assignee: {assignee}");
            }

            if (selectedTask.BlockedBy.Count > 0)
            {
                IReadOnlyList<Guid> blockerIds = selectedTask.BlockedBy.Select(b => b.BlockerTaskId).Distinct().ToList();
                IReadOnlyList<Domain.Entities.TaskItem> blockers = await _tasks.ListAsync(blockerIds, cancellationToken);
                if (blockers.Count > 0)
                {
                    sb.AppendLine("  - Active blockers:");
                    foreach (Domain.Entities.TaskItem blocker in blockers.Where(t => t.Status != Domain.Enums.TaskStatus.Done).Take(8))
                    {
                        sb.AppendLine($"    - {blocker.Title} | {blocker.Priority} | {blocker.Status}");
                    }
                }
            }
        }

        var topTasks = allTasks
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Title)
            .Take(12)
            .ToList();

        if (topTasks.Count > 0)
        {
            sb.AppendLine("- Priority task snapshot:");
            foreach (Domain.Entities.TaskItem task in topTasks)
            {
                sb.AppendLine($"  - {task.Title} | {task.Priority} | {task.Status}");
            }
        }

        return sb.ToString().TrimEnd();
    }
}

