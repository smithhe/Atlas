namespace Atlas.Ui.Services;

/// <summary>In-memory selection IDs mirroring React <c>SelectionState.tsx</c> (not persisted).</summary>
public sealed class SelectionState
{
    public Guid? SelectedTaskId { get; private set; }
    public Guid? SelectedRiskId { get; private set; }
    public Guid? SelectedTeamMemberId { get; private set; }
    public Guid? SelectedProjectId { get; private set; }

    public event Action? Changed;

    public void SelectTask(Guid? taskId)
    {
        if (SelectedTaskId == taskId)
        {
            return;
        }

        SelectedTaskId = taskId;
        Changed?.Invoke();
    }

    public void SelectRisk(Guid? riskId)
    {
        if (SelectedRiskId == riskId)
        {
            return;
        }

        SelectedRiskId = riskId;
        Changed?.Invoke();
    }

    public void SelectTeamMember(Guid? memberId)
    {
        if (SelectedTeamMemberId == memberId)
        {
            return;
        }

        SelectedTeamMemberId = memberId;
        Changed?.Invoke();
    }

    public void SelectProject(Guid? projectId)
    {
        if (SelectedProjectId == projectId)
        {
            return;
        }

        SelectedProjectId = projectId;
        Changed?.Invoke();
    }
}
