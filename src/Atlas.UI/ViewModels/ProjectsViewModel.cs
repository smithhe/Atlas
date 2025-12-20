using System.Collections.ObjectModel;
using System.Linq;
using Atlas.UI.Models;
using ReactiveUI;

namespace Atlas.UI.ViewModels;

public sealed class ProjectsViewModel : PageViewModel
{
    private ProjectItem? _selectedProject;

    public ProjectsViewModel(AiPanelViewModel ai) : base(ai)
    {
        Projects = new ObservableCollection<ProjectItem>
        {
            new() { Name = "Core Platform", Summary = "Shared libraries, refactors, and platform reliability work." },
            new() { Name = "DevEx", Summary = "Developer experience: onboarding, templates, tooling." },
            new() { Name = "Ops", Summary = "Release processes, observability, incident response." },
        };

        SelectedProject = Projects.FirstOrDefault();
    }

    public ObservableCollection<ProjectItem> Projects { get; }

    public ProjectItem? SelectedProject
    {
        get => _selectedProject;
        set => this.RaiseAndSetIfChanged(ref _selectedProject, value);
    }
}


