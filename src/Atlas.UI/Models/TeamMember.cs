using System;
using System.Collections.Generic;

namespace Atlas.UI.Models;

public sealed class TeamMember
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "OK"; // OK/Warning/Critical/Info
    public string CurrentFocus { get; set; } = "";

    public List<PerformanceNote> Notes { get; set; } = new();
    public List<AzureWorkItem> WorkItems { get; set; } = new();

    public DateTimeOffset LastNoteAt { get; set; } = DateTimeOffset.Now;
}

public sealed class PerformanceNote
{
    public DateTimeOffset When { get; set; } = DateTimeOffset.Now;
    public NoteTag Tag { get; set; } = NoteTag.Standup;
    public string Text { get; set; } = "";
    public string? LinkLabel { get; set; }
}

public sealed class AzureWorkItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "In Progress";
    public string? TimeTaken { get; set; }

    public string? TicketUrl { get; set; }
    public string? PrUrl { get; set; }
    public string? GitHistoryUrl { get; set; }
}


