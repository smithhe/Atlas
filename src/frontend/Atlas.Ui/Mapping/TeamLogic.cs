using Atlas.Ui.Models;

namespace Atlas.Ui.Mapping;

/// <summary>
/// Thin stubs mirroring React <c>app/team.ts</c>. Full team pulse logic is Phase 6.
/// </summary>
public static class TeamLogic
{
    static readonly HashSet<string> CurrentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "active",
        "blocked",
        "in progress",
        "code review",
        "in review",
        "ready for review",
        "test acceptance",
        "ui acceptance"
    };

    public static bool IsCurrentTicketStatus(string status) =>
        CurrentStatuses.Contains(status.Trim());

    /// <summary>
    /// Derives Team Pulse / activitySnapshot from existing member data (no persistence).
    /// </summary>
    public static ActivitySnapshot DeriveActivitySnapshot(TeamMember member)
    {
        // TODO(Phase 6): port full deriveActivitySnapshot from React team.ts.
        _ = member;
        return new ActivitySnapshot
        {
            Bullets = Array.Empty<string>(),
            LastUpdatedIso = null,
            QuickTags = null
        };
    }

    public static TeamMember WithDerivedActivitySnapshot(TeamMember member) =>
        new()
        {
            Id = member.Id,
            Name = member.Name,
            Role = member.Role,
            StatusDot = member.StatusDot,
            CurrentFocus = member.CurrentFocus,
            Profile = member.Profile,
            Signals = member.Signals,
            Notes = member.Notes,
            PinnedNoteIds = member.PinnedNoteIds,
            ActivitySnapshot = DeriveActivitySnapshot(member),
            AzureItems = member.AzureItems
        };
}
