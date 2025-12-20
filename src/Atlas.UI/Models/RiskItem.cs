using System;

namespace Atlas.UI.Models;

public sealed class RiskItem
{
    public string Title { get; set; } = "";
    public RiskStatus Status { get; set; } = RiskStatus.Open;
    public string Severity { get; set; } = "Medium"; // lightweight for mock
    public string? Project { get; set; }

    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;

    public string Description { get; set; } = "";
    public string Evidence { get; set; } = "";
    public string NotesHistory { get; set; } = "";
}


