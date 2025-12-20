using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;

namespace Atlas.UI.ViewModels;

public sealed class NavItemViewModel
{
    public NavItemViewModel(string key, string label)
    {
        Key = key;
        Label = label;
    }

    public string Key { get; }
    public string Label { get; }
}

public sealed class MainWindowViewModel : ViewModelBase
    , INavigationHost
{
    private readonly Dictionary<string, PageViewModel> _pages = new();
    private PageViewModel _currentView;
    private NavItemViewModel? _selectedNavItem;
    private string _searchText = "";

    public MainWindowViewModel()
    {
        Ai = new AiPanelViewModel();

        // Pages (seeded)
        _pages["Dashboard"] = new DashboardViewModel(Ai);
        _pages["Tasks"] = new TasksViewModel(Ai, this);
        _pages["Team"] = new TeamViewModel(Ai);
        _pages["Risks"] = new RisksViewModel(Ai);
        _pages["Projects"] = new ProjectsViewModel(Ai);
        _pages["Settings"] = new SettingsViewModel(Ai);

        NavItems = new ObservableCollection<NavItemViewModel>(new[]
        {
            new NavItemViewModel("Dashboard", "Dashboard"),
            new NavItemViewModel("Tasks", "Tasks"),
            new NavItemViewModel("Team", "Team"),
            new NavItemViewModel("Risks", "Risks & Mitigation"),
            new NavItemViewModel("Projects", "Projects"),
            new NavItemViewModel("Settings", "Settings"),
        });

        ToggleAiCommand = ReactiveCommand.Create(() => Ai.IsOpen = !Ai.IsOpen);
        QuickAddCommand = ReactiveCommand.Create(() => Ai.RunPreset("Quick Add (mock)"));

        _currentView = _pages["Dashboard"];
        SelectedNavItem = NavItems.First(x => x.Key == "Dashboard");
    }

    public AiPanelViewModel Ai { get; }

    public ObservableCollection<NavItemViewModel> NavItems { get; }

    public NavItemViewModel? SelectedNavItem
    {
        get => _selectedNavItem;
        set
        {
            if (ReferenceEquals(_selectedNavItem, value))
                return;

            this.RaiseAndSetIfChanged(ref _selectedNavItem, value);

            if (value is null)
                return;

            SwitchTo(value.Key);
        }
    }

    public PageViewModel CurrentView
    {
        get => _currentView;
        private set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ICommand ToggleAiCommand { get; }
    public ICommand QuickAddCommand { get; }

    private void SwitchTo(string key)
    {
        if (!_pages.TryGetValue(key, out var vm))
            return;

        CurrentView = vm;
        ApplyAiContextForKey(key);
    }

    public void Navigate(PageViewModel viewModel)
    {
        CurrentView = viewModel;

        // Keep left-nav selection stable; derive AI context from the target view.
        var key = viewModel switch
        {
            TasksViewModel => "Tasks",
            TaskDetailViewModel => "Tasks",
            DashboardViewModel => "Dashboard",
            TeamViewModel => "Team",
            RisksViewModel => "Risks",
            ProjectsViewModel => "Projects",
            SettingsViewModel => "Settings",
            _ => "Dashboard"
        };

        ApplyAiContextForKey(key);
    }

    private void ApplyAiContextForKey(string key)
    {
        // Context-sensitive AI actions
        Ai.SetContext(
            key switch
            {
                "Dashboard" => "Context: Tasks & Risks",
                "Tasks" => "Context: Tasks",
                "Team" => "Context: Team",
                "Risks" => "Context: Risks",
                "Projects" => "Context: Projects",
                "Settings" => "Context: Settings",
                _ => $"Context: {key}"
            },
            key switch
            {
                "Tasks" => new[] { "Suggest Next Action", "Summarize Incomplete Work", "Reprioritize (draft)" },
                "Team" => new[] { "Summarize patterns", "Highlight growth areas", "Cite specific notes" },
                "Risks" => new[] { "Summarize impact", "Suggest mitigations", "Why this matters (draft)" },
                _ => new[] { "Suggest Next Action", "Summarize Incomplete Work", "Highlight Risks" }
            }
        );
    }
}