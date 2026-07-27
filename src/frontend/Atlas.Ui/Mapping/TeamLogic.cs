using Atlas.Ui.Models;

namespace Atlas.Ui.Mapping;

/// <summary>Team helpers mirroring React <c>app/team.ts</c>.</summary>
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
        var noteTimes = member.Notes.Select(n => n.LastModifiedIso ?? n.CreatedIso);
        var azureTimes = member.AzureItems.Select(a => a.ChangedDateUtc);
        var lastUpdatedIso = MaxIso(noteTimes.Concat(azureTimes));

        var bullets = new List<string>();

        var focus = member.CurrentFocus.Trim();
        if (focus.Length > 0) bullets.Add($"Focus: {focus}");

        var signalSummary = FormatSignalSummary(member.Signals);
        if (signalSummary is not null) bullets.Add(signalSummary);

        var openItems = member.AzureItems.Where(a => IsCurrentTicketStatus(a.Status)).ToList();
        if (openItems.Count == 1)
        {
            bullets.Add($"Active: {openItems[0].Title}");
        }
        else if (openItems.Count > 1)
        {
            bullets.Add($"{openItems.Count} active work items");
        }
        else if (member.AzureItems.Count > 0)
        {
            bullets.Add($"Recent: {member.AzureItems[0].Title}");
        }

        var latestNote = member.Notes
            .OrderByDescending(n => n.LastModifiedIso ?? n.CreatedIso)
            .FirstOrDefault();
        if (latestNote is not null)
        {
            var when = FormatRelativeDays(latestNote.LastModifiedIso ?? latestNote.CreatedIso);
            bullets.Add($"{latestNote.Tag} · {DisplayLabels.GetDerivedTitle(latestNote)} · {when}");
        }

        if (lastUpdatedIso is null && bullets.Count == 0)
        {
            return new ActivitySnapshot
            {
                Bullets = Array.Empty<string>(),
                LastUpdatedIso = null,
                QuickTags = null
            };
        }

        return new ActivitySnapshot
        {
            Bullets = bullets.Take(5).ToList(),
            LastUpdatedIso = lastUpdatedIso,
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

    static string? MaxIso(IEnumerable<string?> values)
    {
        string? best = null;
        var bestMs = double.NegativeInfinity;
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!DateTimeOffset.TryParse(value, out var dto)) continue;
            var ms = dto.ToUnixTimeMilliseconds();
            if (ms > bestMs)
            {
                bestMs = ms;
                best = value;
            }
        }

        return best;
    }

    static string? FormatSignalSummary(TeamMemberSignals signals)
    {
        var parts = new List<string>();
        if (signals.Load == LoadSignal.Heavy) parts.Add("Heavy load");
        else if (signals.Load == LoadSignal.Light) parts.Add("Light load");

        if (signals.Delivery == DeliverySignal.Blocked) parts.Add("Blocked");
        else if (signals.Delivery == DeliverySignal.AtRisk) parts.Add("Delivery at risk");

        if (signals.SupportNeeded == SupportNeededSignal.High) parts.Add("High support need");
        else if (signals.SupportNeeded == SupportNeededSignal.Medium) parts.Add("Medium support need");

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    static string FormatRelativeDays(string iso)
    {
        var days = DisplayLabels.DaysSince(iso);
        if (days is null) return "recently";
        if (days == 0) return "today";
        if (days == 1) return "1 day ago";
        return $"{days} days ago";
    }
}
