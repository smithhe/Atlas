using Atlas.Ui.Models;

namespace Atlas.Ui.Mapping;

/// <summary>Display labels and date helpers mirroring React utils / status strings.</summary>
public static class DisplayLabels
{
    public static string FormatTaskStatus(Models.TaskStatus? status) => status switch
    {
        Models.TaskStatus.NotStarted => "Not Started",
        Models.TaskStatus.InProgress => "In Progress",
        Models.TaskStatus.Blocked => "Blocked",
        Models.TaskStatus.Done => "Done",
        _ => "Not Started"
    };

    public static Models.TaskStatus ParseTaskStatus(string? label) => label switch
    {
        "In Progress" => Models.TaskStatus.InProgress,
        "Blocked" => Models.TaskStatus.Blocked,
        "Done" => Models.TaskStatus.Done,
        _ => Models.TaskStatus.NotStarted
    };

    public static int? DaysSince(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;
        if (!DateTimeOffset.TryParse(iso, out var when)) return null;
        var days = (int)Math.Floor((DateTimeOffset.UtcNow - when.ToUniversalTime()).TotalDays);
        return Math.Max(0, days);
    }

    public static int DaysBetween(string iso, string nowIso)
    {
        var a = DateTimeOffset.Parse(iso).UtcDateTime;
        var b = DateTimeOffset.Parse(nowIso).UtcDateTime;
        return (int)Math.Floor((b - a).TotalDays);
    }

    public static string GetDerivedTitle(TeamNote note)
    {
        var explicitTitle = note.Title?.Trim();
        if (!string.IsNullOrEmpty(explicitTitle)) return explicitTitle;

        var first = note.Text
            .Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Length > 0);
        if (string.IsNullOrEmpty(first)) return "(untitled)";
        return System.Text.RegularExpressions.Regex.Replace(first, @"^#{1,6}\s+", "").Trim();
    }

    public static string FormatDateTime(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return "—";
        if (!DateTimeOffset.TryParse(iso, out var d)) return iso;
        return d.ToLocalTime().ToString("g");
    }

    public static string FormatDateLabel(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return "—";
        if (!DateTimeOffset.TryParse(iso, out var d)) return iso;
        return d.ToString("MMM d, yyyy");
    }
}
