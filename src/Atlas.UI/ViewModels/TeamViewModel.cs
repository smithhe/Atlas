using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Atlas.UI.Models;
using Atlas.UI.Utils;

namespace Atlas.UI.ViewModels;

public sealed class TeamViewModel : PageViewModel
{
    private TeamMember? _selectedMember;
    private string _quickNoteText = "";
    private NoteTag _selectedTag = NoteTag.Standup;
    private string _structuredNoteText = "";

    public TeamViewModel(AiPanelViewModel ai) : base(ai)
    {
        Members = new ObservableCollection<TeamMember>(SeedMembers());
        SelectedMember = Members.FirstOrDefault();

        AddQuickNoteCommand = new RelayCommand(() =>
        {
            if (SelectedMember is null) return;
            if (string.IsNullOrWhiteSpace(QuickNoteText)) return;

            SelectedMember.Notes.Insert(0, new PerformanceNote
            {
                When = DateTimeOffset.Now,
                Tag = NoteTag.Standup,
                Text = QuickNoteText.Trim(),
            });
            SelectedMember.LastNoteAt = DateTimeOffset.Now;
            QuickNoteText = "";
            RaisePropertyChanged(nameof(SelectedMember));
            RaisePropertyChanged(nameof(SelectedMemberNotes));
        });

        AddStructuredNoteCommand = new RelayCommand(() =>
        {
            if (SelectedMember is null) return;
            if (string.IsNullOrWhiteSpace(StructuredNoteText)) return;

            SelectedMember.Notes.Insert(0, new PerformanceNote
            {
                When = DateTimeOffset.Now,
                Tag = SelectedTag,
                Text = StructuredNoteText.Trim(),
                LinkLabel = "ADO #1842 (placeholder)"
            });
            SelectedMember.LastNoteAt = DateTimeOffset.Now;
            StructuredNoteText = "";
            RaisePropertyChanged(nameof(SelectedMember));
            RaisePropertyChanged(nameof(SelectedMemberNotes));
        });
    }

    public ObservableCollection<TeamMember> Members { get; }

    public TeamMember? SelectedMember
    {
        get => _selectedMember;
        set
        {
            if (!SetProperty(ref _selectedMember, value))
                return;

            RaisePropertyChanged(nameof(SelectedMemberNotes));
            RaisePropertyChanged(nameof(SelectedMemberWorkItems));
        }
    }

    public string QuickNoteText
    {
        get => _quickNoteText;
        set => SetProperty(ref _quickNoteText, value);
    }

    public NoteTag SelectedTag
    {
        get => _selectedTag;
        set => SetProperty(ref _selectedTag, value);
    }

    public string StructuredNoteText
    {
        get => _structuredNoteText;
        set => SetProperty(ref _structuredNoteText, value);
    }

    public ObservableCollection<PerformanceNote> SelectedMemberNotes
        => new(SelectedMember?.Notes ?? new());

    public ObservableCollection<AzureWorkItem> SelectedMemberWorkItems
        => new(SelectedMember?.WorkItems ?? new());

    public ICommand AddQuickNoteCommand { get; }
    public ICommand AddStructuredNoteCommand { get; }

    private static TeamMember[] SeedMembers()
    {
        return new[]
        {
            new TeamMember
            {
                Name = "Alice",
                Status = "Ok",
                CurrentFocus = "Working on auth changes",
                LastNoteAt = DateTimeOffset.Now.AddDays(-1),
                Notes =
                {
                    new PerformanceNote { When = DateTimeOffset.Now.AddDays(-1), Tag = NoteTag.Progress, Text = "Auth flow update is ready for review." },
                    new PerformanceNote { When = DateTimeOffset.Now.AddDays(-3), Tag = NoteTag.Standup, Text = "No blockers; pairing with Evan on edge cases." },
                },
                WorkItems =
                {
                    new AzureWorkItem { Id = 1821, Title = "Auth: refresh token", Status = "In Progress", TimeTaken = "2d", TicketUrl = "https://example/ticket/1821" },
                    new AzureWorkItem { Id = 1799, Title = "Login UI polish", Status = "Done", TimeTaken = "4h", PrUrl = "https://example/pr/552" },
                }
            },
            new TeamMember
            {
                Name = "Bob",
                Status = "Warning",
                CurrentFocus = "Refactor tasks",
                LastNoteAt = DateTimeOffset.Now.AddDays(-7),
                Notes =
                {
                    new PerformanceNote { When = DateTimeOffset.Now.AddDays(-7), Tag = NoteTag.Concern, Text = "Scope is unclear; needs a boundary doc." },
                },
                WorkItems =
                {
                    new AzureWorkItem { Id = 1842, Title = "Refactor shared code", Status = "In Progress", TimeTaken = null, TicketUrl = "https://example/ticket/1842" },
                }
            },
            new TeamMember
            {
                Name = "Charlie",
                Status = "Info",
                CurrentFocus = "UI fixes",
                LastNoteAt = DateTimeOffset.Now.AddDays(-2),
                Notes =
                {
                    new PerformanceNote { When = DateTimeOffset.Now.AddDays(-2), Tag = NoteTag.Standup, Text = "Waiting on review for UI tweaks." },
                    new PerformanceNote { When = DateTimeOffset.Now.AddDays(-5), Tag = NoteTag.Praise, Text = "Nice cleanup in navigation styles." },
                },
                WorkItems =
                {
                    new AzureWorkItem { Id = 1760, Title = "Dark theme contrast fixes", Status = "Done", TimeTaken = "1d", PrUrl = "https://example/pr/540" },
                }
            },
            new TeamMember { Name = "Dana", Status = "Ok", CurrentFocus = "ADO hygiene", LastNoteAt = DateTimeOffset.Now.AddDays(-1) },
            new TeamMember { Name = "Evan", Status = "Info", CurrentFocus = "Bug triage", LastNoteAt = DateTimeOffset.Now.AddDays(-4) },
        };
    }
}


