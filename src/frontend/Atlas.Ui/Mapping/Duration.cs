namespace Atlas.Ui.Mapping;

/// <summary>Duration parse/format helpers ported from React <c>app/duration.ts</c>.</summary>
public static class Duration
{
    public sealed record ParsedDuration(int TotalMinutes, DurationParts Parts);

    public sealed record DurationParts(int Days, int Hours, int Minutes);

    /// <summary>
    /// Parses flexible duration text like "1d", "2h30m", "1.5h".
    /// </summary>
    public static ParsedDuration? ParseDurationText(string? input)
    {
        var raw = (input ?? "").Trim();
        if (raw.Length == 0)
        {
            return null;
        }

        var s = string.Concat(raw.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();
        var tokenRe = new System.Text.RegularExpressions.Regex(@"(\d+(?:\.\d+)?)([dhm])");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var consumed = 0;
        double days = 0, hours = 0, minutes = 0;

        foreach (System.Text.RegularExpressions.Match match in tokenRe.Matches(s))
        {
            if (!double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var value) || value < 0)
            {
                return null;
            }

            var unit = match.Groups[2].Value;
            if (!seen.Add(unit))
            {
                return null;
            }

            consumed += match.Length;
            switch (unit)
            {
                case "d": days = value; break;
                case "h": hours = value; break;
                case "m": minutes = value; break;
            }
        }

        if (consumed != s.Length)
        {
            return null;
        }

        var totalMinutes = (int)Math.Round(days * 24 * 60) + (int)Math.Round(hours * 60) + (int)Math.Round(minutes);
        if (totalMinutes <= 0)
        {
            return null;
        }

        var normDays = totalMinutes / (24 * 60);
        var remAfterDays = totalMinutes - normDays * 24 * 60;
        var normHours = remAfterDays / 60;
        var normMinutes = remAfterDays - normHours * 60;
        return new ParsedDuration(totalMinutes, new DurationParts(normDays, normHours, normMinutes));
    }

    public static string FormatDurationFromMinutes(double totalMinutes)
    {
        if (double.IsNaN(totalMinutes) || double.IsInfinity(totalMinutes) || totalMinutes <= 0)
        {
            return "—";
        }

        var mins = (int)Math.Round(totalMinutes);
        var days = mins / (24 * 60);
        var remAfterDays = mins - days * 24 * 60;
        var hours = remAfterDays / 60;
        var minutes = remAfterDays - hours * 60;

        var parts = new List<string>();
        if (days > 0)
        {
            parts.Add($"{days}d");
        }

        if (hours > 0)
        {
            parts.Add($"{hours}h");
        }

        if (minutes > 0)
        {
            parts.Add($"{minutes}m");
        }

        return parts.Count == 0 ? "—" : string.Join(' ', parts);
    }
}
