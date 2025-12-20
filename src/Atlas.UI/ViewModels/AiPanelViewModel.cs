using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;

namespace Atlas.UI.ViewModels;

public sealed class AiActionItem
{
    public AiActionItem(string title, ICommand command)
    {
        Title = title;
        Command = command;
    }

    public string Title { get; }
    public ICommand Command { get; }
}

public sealed class AiPanelViewModel : ViewModelBase
{
    private bool _isOpen;
    private string _contextTitle = "Context: Dashboard";
    private string _output = "";

    public AiPanelViewModel()
    {
        SuggestNextActionCommand = ReactiveCommand.Create(() => RunPreset("Suggest Next Action"));
        SummarizeIncompleteWorkCommand = ReactiveCommand.Create(() => RunPreset("Summarize Incomplete Work"));
        HighlightRisksCommand = ReactiveCommand.Create(() => RunPreset("Highlight Risks"));

        InsertDraftCommand = ReactiveCommand.Create(() => Output = Output.Length == 0
            ? "(Nothing to insert yet.)"
            : $"[Draft inserted into editor]\n\n{Output}");

        CopyCommand = ReactiveCommand.Create(() => Output = Output.Length == 0
            ? "(Nothing to copy yet.)"
            : $"{Output}\n\n[Copied to clipboard: placeholder]");

        CloseCommand = ReactiveCommand.Create(() => IsOpen = false);

        Actions = new ObservableCollection<AiActionItem>
        {
            new("Suggest Next Action", SuggestNextActionCommand),
            new("Summarize Incomplete Work", SummarizeIncompleteWorkCommand),
            new("Highlight Risks", HighlightRisksCommand),
        };
    }

    public bool IsOpen
    {
        get => _isOpen;
        set => this.RaiseAndSetIfChanged(ref _isOpen, value);
    }

    public string ContextTitle
    {
        get => _contextTitle;
        set => this.RaiseAndSetIfChanged(ref _contextTitle, value);
    }

    public ObservableCollection<AiActionItem> Actions { get; }

    public string Output
    {
        get => _output;
        set => this.RaiseAndSetIfChanged(ref _output, value);
    }

    public ICommand SuggestNextActionCommand { get; }
    public ICommand SummarizeIncompleteWorkCommand { get; }
    public ICommand HighlightRisksCommand { get; }

    public ICommand InsertDraftCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand CloseCommand { get; }

    public void RunPreset(string presetName)
    {
        IsOpen = true;
        Output = presetName switch
        {
            "Suggest Next Action" =>
                "Draft plan:\n- Prepare sprint review (high priority)\n- Address shared code risk (align on refactor scope)\n- Update onboarding docs (overdue)\n\nRationale: tackles highest impact + clears blockers.",
            "Summarize Incomplete Work" =>
                "Incomplete work (draft):\n- Refactor proposal review: waiting on clarifications from Bob\n- PR reviews: 3 pending, one is security-related\n- Onboarding docs: no activity in 10 days\n\nSuggested next: schedule 30m refactor sync + batch PR reviews.",
            "Highlight Risks" =>
                "Top risks (draft):\n- Inconsistent shared code changes (impact: regressions)\n  Mitigation: lock branch + agree on refactor boundaries + add CI gates\n- Onboarding drift (impact: ramp-up time)\n  Mitigation: assign owner + weekly refresh reminder",
            _ => $"[{presetName}] (mock output)"
        };
    }

    public void SetContext(string contextTitle, params string[] actionTitles)
    {
        ContextTitle = contextTitle;
        Actions.Clear();

        foreach (var title in actionTitles)
        {
            Actions.Add(title switch
            {
                "Suggest Next Action" => new AiActionItem(title, SuggestNextActionCommand),
                "Summarize Incomplete Work" => new AiActionItem(title, SummarizeIncompleteWorkCommand),
                "Highlight Risks" => new AiActionItem(title, HighlightRisksCommand),
                _ => new AiActionItem(title, ReactiveCommand.Create(() => RunPreset(title)))
            });
        }
    }
}


