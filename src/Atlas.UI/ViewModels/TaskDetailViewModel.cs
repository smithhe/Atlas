using System;
using System.Windows.Input;
using Atlas.UI.Models;
using Atlas.UI.Utils;

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

        BackCommand = new RelayCommand(() => _navigation.Navigate(_tasksViewModel));

        TouchCommand = new RelayCommand(() =>
        {
            Task.LastTouched = DateTimeOffset.Now;
            RaisePropertyChanged(nameof(Task));
            RaisePropertyChanged(nameof(EstimatedPreview));
            RaisePropertyChanged(nameof(LastTouchedDisplay));
            RaisePropertyChanged(nameof(IsStale));
        });

        SaveCommand = new RelayCommand(() => Ai.RunPreset("Save (mock)"));
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


