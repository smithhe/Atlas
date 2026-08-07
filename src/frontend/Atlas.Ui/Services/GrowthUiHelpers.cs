using Atlas.Ui.Api.Generated;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

static class GrowthUiHelpers
{
    public const int MaxSkillLength = 200;
    public const int MaxGoalTitle = 200;
    public const int MaxGoalDescription = 5000;
    public const int MaxGoalCategory = 200;
    public const int MaxSuccessCriteriaAggregate = 5000;
    public const int MaxActionEvidence = 5000;
    public const int MaxThemeTitle = 200;
    public const int MaxThemeDescription = 2000;
    public const int MaxThemeObservedLabel = 200;
    public const int MaxFocusMarkdown = 10000;
    public const int MaxActionTitle = 200;
    public const int MaxActionNotes = 5000;
    public const int MaxCheckInNote = 5000;

    public static string FormatUserError(string context, Exception ex)
    {
        if (ex is AtlasApiException apiEx)
        {
            return apiEx.StatusCode switch
            {
                400 => $"{context} Please check your entries and try again.",
                404 => $"{context} The item could not be found.",
                >= 500 => $"{context} The server is unavailable. Please try again shortly.",
                _ => $"{context} Please try again."
            };
        }

        return $"{context} Please try again.";
    }

    public static string GrowthUnavailableMessage(string operation) =>
        $"Unable to {operation} because the growth record is not available. Try Retry or refresh the page.";

    public static string MissingCreatedIdMessage(string entityName) =>
        $"The server did not return a valid {entityName} id. Refreshing growth data.";

    public static bool IsValidCreatedId(Guid? id) => id is Guid value && value != Guid.Empty;

    public static bool IsTargetBeforeStart(string? startIso, string? targetIso)
    {
        if (string.IsNullOrWhiteSpace(startIso) || string.IsNullOrWhiteSpace(targetIso))
            return false;
        if (!DateTimeOffset.TryParse(startIso, out var start))
            return false;
        if (!DateTimeOffset.TryParse(targetIso, out var target))
            return false;
        return target.Date < start.Date;
    }

    public static string? LengthError(string? value, int max, string fieldName) =>
        value is not null && value.Length > max ? $"{fieldName} cannot exceed {max} characters." : null;

    public static string? ValidateGoalAdd(string title, string description, string? category, string? startIso, string? targetIso)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required.";
        if (LengthError(title, MaxGoalTitle, "Title") is { } titleError)
            return titleError;
        if (LengthError(description, MaxGoalDescription, "Description") is { } descError)
            return descError;
        if (LengthError(category, MaxGoalCategory, "Category") is { } categoryError)
            return categoryError;
        if (IsTargetBeforeStart(startIso, targetIso))
            return "Target date must be on or after the start date.";
        return null;
    }

    public static string? ValidateSuccessCriteria(IReadOnlyList<string> criteria) =>
        LengthError(string.Join('\n', criteria), MaxSuccessCriteriaAggregate, "Success criteria");

    public static string? ValidateProgressPercent(int? percent)
    {
        if (percent is null)
            return null;
        if (percent < 0 || percent > 100)
            return "Progress must be between 0 and 100.";
        return null;
    }

    public static string? ValidateActionEvidence(IReadOnlyList<string> links) =>
        LengthError(string.Join('\n', links), MaxActionEvidence, "Evidence / links");

    public static string? ValidateGoalPersist(GrowthGoal goal)
    {
        if (string.IsNullOrWhiteSpace(goal.Title))
            return "Title is required.";
        if (LengthError(goal.Title, MaxGoalTitle, "Title") is { } titleError)
            return titleError;
        if (LengthError(goal.Description, MaxGoalDescription, "Description") is { } descError)
            return descError;
        if (LengthError(goal.Summary, MaxGoalDescription, "Summary") is { } summaryError)
            return summaryError;
        if (LengthError(goal.Category, MaxGoalCategory, "Category") is { } categoryError)
            return categoryError;
        if (ValidateSuccessCriteria(goal.SuccessCriteria) is { } criteriaError)
            return criteriaError;
        if (ValidateProgressPercent(goal.ProgressPercent) is { } progressError)
            return progressError;
        if (IsTargetBeforeStart(goal.StartDateIso, goal.TargetDateIso))
            return "Target date must be on or after the start date.";
        return null;
    }

    public static string? ValidateThemeFields(string title, string description, string? observedLabel)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required.";
        if (LengthError(title, MaxThemeTitle, "Title") is { } titleError)
            return titleError;
        if (LengthError(description, MaxThemeDescription, "Description") is { } descError)
            return descError;
        if (LengthError(observedLabel, MaxThemeObservedLabel, "Observed since") is { } observedError)
            return observedError;
        return null;
    }

    public static string? ValidateFocusMarkdown(string markdown) =>
        LengthError(markdown, MaxFocusMarkdown, "Focus areas");

    public static string? ValidateActionPersist(GrowthGoalAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Title))
            return "Action title is required.";
        if (LengthError(action.Title, MaxActionTitle, "Action title") is { } titleError)
            return titleError;
        if (LengthError(action.Notes, MaxActionNotes, "Action notes") is { } notesError)
            return notesError;
        if (ValidateActionEvidence(action.Links) is { } evidenceError)
            return evidenceError;
        return null;
    }

    public static string? ValidateCheckInPersist(GrowthGoalCheckIn checkIn)
    {
        if (string.IsNullOrWhiteSpace(checkIn.Note))
            return "Note is required before changes are saved.";
        if (LengthError(checkIn.Note, MaxCheckInNote, "Check-in note") is { } noteError)
            return noteError;
        return null;
    }

    public static (string Label, string From, string To) ParseSkillText(string input)
    {
        var raw = input.Trim();
        if (string.IsNullOrEmpty(raw))
            return ("", "", "");

        const string arrow = "→";
        var hasArrow = raw.Contains(arrow);
        var parts = hasArrow ? raw.Split(arrow, 2) : new[] { raw, "" };
        var left = parts[0].Trim();
        var to = parts.Length > 1 ? parts[1].Trim() : "";
        var colonIdx = left.IndexOf(':');
        if (colonIdx >= 0)
            return (left[..colonIdx].Trim(), left[(colonIdx + 1)..].Trim(), to);
        return (left, "", to);
    }

    public static string FormatSkillText(string label, string from, string to)
    {
        label = label.Trim();
        from = from.Trim();
        to = to.Trim();
        if (string.IsNullOrEmpty(label))
            return "";
        if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
            return $"{label}: {from} → {to}";
        if (!string.IsNullOrEmpty(from))
            return $"{label}: {from}";
        if (!string.IsNullOrEmpty(to))
            return $"{label} → {to}";
        return label;
    }

    public static bool HasSkillDuplicate(IReadOnlyList<string> skills, string candidate, int? excludeIndex)
    {
        for (var i = 0; i < skills.Count; i++)
        {
            if (excludeIndex == i)
                continue;
            if (string.Equals(skills[i].Trim(), candidate.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
