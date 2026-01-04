using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;

namespace Atlas.Domain.Entities;

public sealed class Settings : AggregateRoot
{
    public int StaleDays { get; set; }
    public bool DefaultAiManualOnly { get; set; }
    public Theme Theme { get; set; }

    public string? AzureDevOpsBaseUrl { get; set; }
}

