using Atlas.Application.Features.TeamMembers.AzureWorkItems.AddAzureWorkItemLocalNote;
using Atlas.Application.Features.TeamMembers.Notes.DeleteTeamNote;
using Atlas.Application.Features.TeamMembers.Notes.SetPinnedNotes;
using Atlas.Application.Features.TeamMembers.Notes.UpdateTeamNote;
using Atlas.Application.Features.TeamMembers.Profile.UpdateTeamMemberProfile;
using Atlas.Application.Features.TeamMembers.Risks.AddTeamMemberRisk;
using Atlas.Application.Features.TeamMembers.Risks.DeleteTeamMemberRisk;
using Atlas.Application.Features.TeamMembers.Risks.UpdateTeamMemberRisk;
using Atlas.Application.Features.TeamMembers.Signals.UpdateTeamMemberSignals;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.TeamMembers;

public sealed class TeamMemberCommandValidatorTests
{
    private static readonly Guid TeamMemberId = Guid.NewGuid();
    private static readonly Guid NoteId = Guid.NewGuid();
    private static readonly Guid RiskId = Guid.NewGuid();

    [Fact]
    public void UpdateTeamNote_WhenTextEmpty_Fails()
    {
        var validator = new UpdateTeamNoteCommandValidator();
        ValidationResult result = validator.Validate(new UpdateTeamNoteCommand(TeamMemberId, NoteId, NoteType.Standup, null, string.Empty, null));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void DeleteTeamNote_WhenNoteIdEmpty_Fails()
    {
        var validator = new DeleteTeamNoteCommandValidator();
        ValidationResult result = validator.Validate(new DeleteTeamNoteCommand(TeamMemberId, Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetPinnedNotes_WhenNoteIdEmpty_Fails()
    {
        var validator = new SetPinnedNotesCommandValidator();
        ValidationResult result = validator.Validate(new SetPinnedNotesCommand(TeamMemberId, [Guid.Empty]));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateTeamMemberProfile_WhenValid_Passes()
    {
        var validator = new UpdateTeamMemberProfileCommandValidator();
        ValidationResult result = validator.Validate(new UpdateTeamMemberProfileCommand(TeamMemberId, "UTC", "9-5"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateTeamMemberSignals_WhenTeamMemberIdEmpty_Fails()
    {
        var validator = new UpdateTeamMemberSignalsCommandValidator();
        ValidationResult result = validator.Validate(new UpdateTeamMemberSignalsCommand(
            Guid.Empty, LoadSignal.Normal, DeliverySignal.OnTrack, SupportNeededSignal.Low));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddTeamMemberRisk_WhenTitleEmpty_Fails()
    {
        var validator = new AddTeamMemberRiskCommandValidator();
        ValidationResult result = validator.Validate(ValidTeamMemberRisk() with { Title = string.Empty });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddTeamMemberRisk_WhenValid_Passes()
    {
        var validator = new AddTeamMemberRiskCommandValidator();
        ValidationResult result = validator.Validate(ValidTeamMemberRisk());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateTeamMemberRisk_WhenRiskIdEmpty_Fails()
    {
        var validator = new UpdateTeamMemberRiskCommandValidator();
        ValidationResult result = validator.Validate(ValidTeamMemberRiskUpdate() with { TeamMemberRiskId = Guid.Empty });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void DeleteTeamMemberRisk_WhenTeamMemberRiskIdEmpty_Fails()
    {
        var validator = new DeleteTeamMemberRiskCommandValidator();
        ValidationResult result = validator.Validate(new DeleteTeamMemberRiskCommand(TeamMemberId, Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddAzureWorkItemLocalNote_WhenTextEmpty_Fails()
    {
        var validator = new AddAzureWorkItemLocalNoteCommandValidator();
        ValidationResult result = validator.Validate(new AddAzureWorkItemLocalNoteCommand(TeamMemberId, 42, string.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddAzureWorkItemLocalNote_WhenValid_Passes()
    {
        var validator = new AddAzureWorkItemLocalNoteCommandValidator();
        ValidationResult result = validator.Validate(new AddAzureWorkItemLocalNoteCommand(TeamMemberId, 42, "Local note"));
        Assert.True(result.IsValid);
    }

    private static AddTeamMemberRiskCommand ValidTeamMemberRisk() => new(
        TeamMemberId,
        "Burnout risk",
        TeamMemberRiskSeverity.High,
        "Wellbeing",
        TeamMemberRiskStatus.Open,
        TeamMemberRiskTrend.Worsening,
        DateOnly.FromDateTime(DateTime.UtcNow),
        "Delivery",
        "Signs of fatigue",
        "Reduce load",
        null);

    private static UpdateTeamMemberRiskCommand ValidTeamMemberRiskUpdate() => new(
        TeamMemberId,
        RiskId,
        "Burnout risk",
        TeamMemberRiskSeverity.Medium,
        "Wellbeing",
        TeamMemberRiskStatus.Mitigating,
        TeamMemberRiskTrend.Stable,
        DateOnly.FromDateTime(DateTime.UtcNow),
        "Delivery",
        "Improving",
        "Continue monitoring",
        null,
        null);
}
