using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Atlas.UI.Models;
using ReactiveUI;

namespace Atlas.UI.ViewModels;

public sealed class RisksViewModel : PageViewModel
{
    private RiskItem? _selectedRisk;
    private RiskStatus _statusFilter = RiskStatus.Open;
    private string _projectFilter = "";

    public RisksViewModel(AiPanelViewModel ai) : base(ai)
    {
        Risks = new ObservableCollection<RiskItem>
        {
            new()
            {
                Title = "Inconsistent shared code changes",
                Status = RiskStatus.Open,
                Severity = "High",
                Project = "Core Platform",
                LastUpdated = DateTimeOffset.Now.AddDays(-1),
                Description = "Multiple parallel changes are landing without clear boundaries, increasing regression risk.",
                Evidence = "- PRs touching shared code without coordination\n- Conflicting refactor approaches",
                NotesHistory = $"{DateTimeOffset.Now.AddDays(-2):d}: Noticed duplicated logic in shared modules.\n{DateTimeOffset.Now.AddDays(-1):d}: Suggested refactor boundary doc."
            },
            new()
            {
                Title = "Onboarding drift",
                Status = RiskStatus.Watching,
                Severity = "Medium",
                Project = "DevEx",
                LastUpdated = DateTimeOffset.Now.AddDays(-10),
                Description = "Docs are out of date; new dev setup takes longer than expected.",
                Evidence = "- No activity in 10 days\n- New hires report missing steps",
                NotesHistory = $"{DateTimeOffset.Now.AddDays(-10):d}: Flagged stale docs."
            },
            new()
            {
                Title = "Release checklist not followed",
                Status = RiskStatus.Resolved,
                Severity = "Low",
                Project = "Ops",
                LastUpdated = DateTimeOffset.Now.AddDays(-3),
                Description = "Checklist was skipped during hotfix.",
                Evidence = "- Missing sign-off log entry",
                NotesHistory = $"{DateTimeOffset.Now.AddDays(-3):d}: Added guardrail reminder to pipeline."
            },
        };

        SelectedRisk = Risks.FirstOrDefault();

        SaveCommand = ReactiveCommand.Create(() => Ai.RunPreset("Save (mock)"));
    }

    public ObservableCollection<RiskItem> Risks { get; }

    public RiskItem? SelectedRisk
    {
        get => _selectedRisk;
        set => this.RaiseAndSetIfChanged(ref _selectedRisk, value);
    }

    public RiskStatus StatusFilter
    {
        get => _statusFilter;
        set => this.RaiseAndSetIfChanged(ref _statusFilter, value);
    }

    public string ProjectFilter
    {
        get => _projectFilter;
        set => this.RaiseAndSetIfChanged(ref _projectFilter, value);
    }

    public ICommand SaveCommand { get; }
}


