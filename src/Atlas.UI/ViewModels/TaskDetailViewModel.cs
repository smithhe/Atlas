using System;
using System.Windows.Input;
using Atlas.UI.Models;
using ReactiveUI;

namespace Atlas.UI.ViewModels;

public sealed class TaskDetailViewModel : PageViewModel
{
    private readonly INavigationHost _navigation;
    private readonly TasksViewModel _tasksViewModel;

    public TaskDetailViewModel(AiPanelViewModel ai, INavigationHost navigation, TasksViewModel tasksViewModel, TaskItem task)
        : base(ai)
    {
        _navigation = navigation;
        _tasksViewModel = tasksViewModel;
        Task = task;

        BackCommand = ReactiveCommand.Create(() => _navigation.Navigate(_tasksViewModel));

        TouchCommand = ReactiveCommand.Create(() =>
        {
            Task.LastTouched = DateTimeOffset.Now;
            this.RaisePropertyChanged(nameof(Task));
            this.RaisePropertyChanged(nameof(EstimatedPreview));
            this.RaisePropertyChanged(nameof(LastTouchedDisplay));
            this.RaisePropertyChanged(nameof(IsStale));
        });

        SaveCommand = ReactiveCommand.Create(() => Ai.RunPreset("Save (mock)"));
    }

    public TaskItem Task { get; }

    public string EstimatedPreview
    {
        get
        {
            var d = Task.EstimatedDays;
            var h = Task.EstimatedHours;
            if (d <= 0 && h <= 0) return "Estimated: \u2014";
            if (d > 0 && h > 0) return $"Estimated: {d}d {h}h";
            if (d > 0) return $"Estimated: {d}d";
            return $"Estimated: {h}h";
        }
    }

    public string LastTouchedDisplay => $"Last touched: {Task.LastTouched:g}";

    public bool IsStale => (DateTimeOffset.Now - Task.LastTouched).TotalDays >= 10;

    public ICommand BackCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand TouchCommand { get; }
}


