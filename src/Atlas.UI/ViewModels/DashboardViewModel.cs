using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Atlas.UI.Utils;

namespace Atlas.UI.ViewModels;

public sealed class DashboardAttentionItem
{
    public string Severity { get; set; } = "Info"; // Critical/Warning/Ok/Info
    public string Title { get; set; } = "";
    public string Meta { get; set; } = "";
    public string Type { get; set; } = "Task"; // Task/Risk/Team
}

public sealed class DashboardTaskItem
{
    public string Title { get; set; } = "";
    public string Meta { get; set; } = "";
    public string Estimate { get; set; } = "";
    public string Priority { get; set; } = "Medium";
}

public sealed class TeamPulseItem
{
    public string Status { get; set; } = "Info";
    public string Name { get; set; } = "";
    public string Focus { get; set; } = "";
    public string Note { get; set; } = "";
}

public sealed class DashboardViewModel : PageViewModel
{
    public DashboardViewModel(AiPanelViewModel ai) : base(ai)
    {
        // Seed data per spec
        AttentionRequired = new ObservableCollection<DashboardAttentionItem>
        {
            new() { Severity = "Critical", Title = "Review refactor proposal", Meta = "High, 2 days", Type = "Task" },
            new() { Severity = "Warning", Title = "Risk: Inconsistent shared code changes", Meta = "Open", Type = "Risk" },
            new() { Severity = "Warning", Title = "Update onboarding docs", Meta = "No activity in 10 days", Type = "Task" },
            new() { Severity = "Info", Title = "Bob: No notes since last standup", Meta = "Stale 7d", Type = "Team" },
        };

        TodayThisWeek = new ObservableCollection<DashboardTaskItem>
        {
            new() { Title = "Prepare sprint review", Priority = "High", Meta = "Today", Estimate = "3h" },
            new() { Title = "Support team unblock", Priority = "Medium", Meta = "This week", Estimate = "1h" },
            new() { Title = "Review pull requests", Priority = "Medium", Meta = "This week", Estimate = "2h" },
        };

        TeamPulse = new ObservableCollection<TeamPulseItem>
        {
            new() { Status = "Ok", Name = "Alice", Focus = "Working on auth changes", Note = "No blockers" },
            new() { Status = "Warning", Name = "Bob", Focus = "Refactor tasks", Note = "Scope unclear" },
            new() { Status = "Info", Name = "Charlie", Focus = "UI fixes", Note = "Waiting on review" },
            new() { Status = "Ok", Name = "Dana", Focus = "ADO cleanup", Note = "On track" },
            new() { Status = "Info", Name = "Evan", Focus = "Bug triage", Note = "Watching incidents" },
        };

        AskAiWhatNextCommand = new RelayCommand(() =>
        {
            Ai.IsOpen = true;
            Ai.RunPreset("Suggest Next Action");
        });

        StaleThresholdDays = 10;
        LastRefreshedAt = DateTimeOffset.Now;
    }

    public int StaleThresholdDays { get; }
    public DateTimeOffset LastRefreshedAt { get; }

    public ObservableCollection<DashboardAttentionItem> AttentionRequired { get; }
    public ObservableCollection<DashboardTaskItem> TodayThisWeek { get; }
    public ObservableCollection<TeamPulseItem> TeamPulse { get; }

    public ICommand AskAiWhatNextCommand { get; }
}


