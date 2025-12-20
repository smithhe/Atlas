using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Atlas.UI.Models;
using Atlas.UI.Utils;

namespace Atlas.UI.ViewModels;

public sealed class TasksViewModel : PageViewModel
{
    private TaskItem? _selectedTask;
    private string _projectFilter = "";
    private string _riskFilter = "";
    private Priority _priorityFilter = Priority.Medium;
    private readonly INavigationHost _navigation;

    public TasksViewModel(AiPanelViewModel ai, INavigationHost navigation) : base(ai)
    {
        _navigation = navigation;

        Tasks = new ObservableCollection<TaskItem>
        {
            new()
            {
                Title = "Review refactor proposal",
                Priority = Priority.High,
                EstimatedDays = 2,
                EstimatedHours = 0,
                Project = "Core Platform",
                Risk = "Refactor justification",
                LastTouched = DateTimeOffset.Now.AddDays(-1),
                Notes = "Need to confirm scope + rollout plan. Ask Bob for a 1-page summary."
            },
            new()
            {
                Title = "Update onboarding docs",
                Priority = Priority.Medium,
                EstimatedDays = 1,
                EstimatedHours = 0,
                Project = "DevEx",
                Risk = "Onboarding drift",
                LastTouched = DateTimeOffset.Now.AddDays(-10),
                Notes = "Refresh local dev steps + add troubleshooting section."
            },
            new()
            {
                Title = "Review pull requests",
                Priority = Priority.Medium,
                EstimatedDays = 0,
                EstimatedHours = 2,
                Project = null,
                Risk = null,
                LastTouched = DateTimeOffset.Now.AddHours(-6),
                Notes = "Batch review after lunch. Focus: auth + UI polish."
            },
        };

        SelectedTask = Tasks.FirstOrDefault();

        OpenTaskCommand = new RelayCommand<TaskItem>(task =>
        {
            if (task is null) return;
            SelectedTask = task;
            _navigation.Navigate(new TaskDetailViewModel(Ai, _navigation, this, task));
        });

        TouchCommand = new RelayCommand(() =>
        {
            if (SelectedTask is null) return;
            SelectedTask.LastTouched = DateTimeOffset.Now;
            RaisePropertyChanged(nameof(SelectedTask));
            RaisePropertyChanged(nameof(SelectedTaskLastTouchedDisplay));
            RaisePropertyChanged(nameof(SelectedTaskIsStale));
        });

        SaveCommand = new RelayCommand(() => Ai.RunPreset("Save (mock)"));
    }

    public ObservableCollection<TaskItem> Tasks { get; }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (!SetProperty(ref _selectedTask, value))
                return;

            RaisePropertyChanged(nameof(SelectedTaskEstimatedPreview));
            RaisePropertyChanged(nameof(SelectedTaskLastTouchedDisplay));
            RaisePropertyChanged(nameof(SelectedTaskIsStale));
        }
    }

    public string ProjectFilter
    {
        get => _projectFilter;
        set => SetProperty(ref _projectFilter, value);
    }

    public string RiskFilter
    {
        get => _riskFilter;
        set => SetProperty(ref _riskFilter, value);
    }

    public Priority PriorityFilter
    {
        get => _priorityFilter;
        set => SetProperty(ref _priorityFilter, value);
    }

    public string SelectedTaskEstimatedPreview
    {
        get
        {
            if (SelectedTask is null) return "";
            var d = SelectedTask.EstimatedDays;
            var h = SelectedTask.EstimatedHours;
            if (d <= 0 && h <= 0) return "Estimated: —";
            if (d > 0 && h > 0) return $"Estimated: {d}d {h}h";
            if (d > 0) return $"Estimated: {d}d";
            return $"Estimated: {h}h";
        }
    }

    public string SelectedTaskLastTouchedDisplay
        => SelectedTask is null ? "" : $"Last touched: {SelectedTask.LastTouched:g}";

    public bool SelectedTaskIsStale
        => SelectedTask is not null && (DateTimeOffset.Now - SelectedTask.LastTouched).TotalDays >= 10;

    public ICommand SaveCommand { get; }
    public ICommand TouchCommand { get; }
    public ICommand OpenTaskCommand { get; }
}


