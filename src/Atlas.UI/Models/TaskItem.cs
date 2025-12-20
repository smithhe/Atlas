using System;

namespace Atlas.UI.Models;

public sealed class TaskItem
{
    public string Title { get; set; } = "";
    public Priority Priority { get; set; } = Priority.Medium;
    public string? Project { get; set; }
    public string? Risk { get; set; }

    public int EstimatedDays { get; set; }
    public int EstimatedHours { get; set; }

    public string EstimateDisplay
    {
        get
        {
            var d = EstimatedDays;
            var h = EstimatedHours;
            if (d <= 0 && h <= 0) return "\u2014";
            if (d > 0 && h > 0) return $"{d}d {h}h";
            if (d > 0) return $"{d}d";
            return $"{h}h";
        }
    }

    public DateTimeOffset LastTouched { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? DueDate { get; set; }

    public string Notes { get; set; } = "";
}


