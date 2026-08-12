using Atlas.Ui.Models;

namespace Atlas.Ui.Mapping;

/// <summary>CSS tone class helpers ported from React <c>app/tones.ts</c>.</summary>
public static class Tones
{
    public static string TaskStatusTone(Models.TaskStatus? status) => status switch
    {
        Models.TaskStatus.Done => "toneGood",
        Models.TaskStatus.Blocked => "toneBad",
        Models.TaskStatus.InProgress => "toneInfo",
        _ => "toneNeutral"
    };

    public static string PriorityTone(Priority? priority)
    {
        if (priority is null or Priority.Low)
        {
            return "toneNeutral";
        }

        if (priority is Priority.Medium)
        {
            return "toneWarn";
        }

        return "toneBad";
    }

    public static string RiskStatusTone(RiskStatus status) => status switch
    {
        RiskStatus.Open => "toneBad",
        RiskStatus.Watching => "toneWarn",
        RiskStatus.Resolved => "toneGood",
        _ => "toneNeutral"
    };

    public static string SeverityTone(string? severity) => severity switch
    {
        "High" => "toneBad",
        "Medium" => "toneWarn",
        _ => "toneNeutral"
    };

    public static string HealthTone(HealthSignal? health) => health switch
    {
        HealthSignal.Green => "toneGood",
        HealthSignal.Yellow => "toneWarn",
        HealthSignal.Red => "toneBad",
        _ => "toneNeutral"
    };

    public static string ProjectStatusTone(string? status) => status switch
    {
        "Active" => "toneGood",
        "Paused" => "toneWarn",
        _ => "toneNeutral"
    };

    public static string GoalStatusTone(GrowthGoalStatus status) => status switch
    {
        GrowthGoalStatus.Completed or GrowthGoalStatus.OnTrack => "toneGood",
        GrowthGoalStatus.NeedsAttention => "toneWarn",
        _ => "toneNeutral"
    };

    public static string ActionStateTone(GrowthGoalActionState state) => state switch
    {
        GrowthGoalActionState.Complete => "toneGood",
        GrowthGoalActionState.InProgress => "toneWarn",
        _ => "toneNeutral"
    };

    public static string CheckInSignalTone(GrowthGoalCheckInSignal signal) => signal switch
    {
        GrowthGoalCheckInSignal.Positive => "toneGood",
        GrowthGoalCheckInSignal.Mixed => "toneWarn",
        GrowthGoalCheckInSignal.Concern => "toneBad",
        _ => "toneNeutral"
    };

    public static string SignalTone(string value)
    {
        var v = value.ToLowerInvariant();
        if (v.Contains("blocked"))
        {
            return "toneBad";
        }

        if (v.Contains("atrisk") || v.Contains("heavy") || v == "high" || v.Contains("medium"))
        {
            return "toneWarn";
        }

        if (v.Contains("ontrack") || v.Contains("light") || v == "low")
        {
            return "toneGood";
        }

        return "toneNeutral";
    }

    public static string TicketAttentionTone(string status)
    {
        var s = status.ToLowerInvariant();
        if (s.Contains("blocked"))
        {
            return "toneBad";
        }

        if (s.Contains("code review") || s.Contains("in review") || s.Contains("review"))
        {
            return "toneWarn";
        }

        return "toneNeutral";
    }
}
