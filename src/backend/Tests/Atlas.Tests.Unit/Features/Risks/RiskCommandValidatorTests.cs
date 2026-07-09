using Atlas.Application.Features.Risks.History.AddRiskHistoryEntry;
using Atlas.Application.Features.Risks.History.DeleteRiskHistoryEntry;
using Atlas.Application.Features.Risks.History.UpdateRiskHistoryEntry;
using Atlas.Application.Features.Risks.TeamMembers.SetRiskTeamMembers;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Risks;

public sealed class RiskCommandValidatorTests
{
    private static readonly Guid RiskId = Guid.NewGuid();
    private static readonly Guid EntryId = Guid.NewGuid();

    [Fact]
    public void AddRiskHistoryEntry_WhenTextEmpty_Fails()
    {
        var validator = new AddRiskHistoryEntryCommandValidator();
        ValidationResult result = validator.Validate(new AddRiskHistoryEntryCommand(RiskId, string.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddRiskHistoryEntry_WhenValid_Passes()
    {
        var validator = new AddRiskHistoryEntryCommandValidator();
        ValidationResult result = validator.Validate(new AddRiskHistoryEntryCommand(RiskId, "Mitigation started"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateRiskHistoryEntry_WhenEntryIdEmpty_Fails()
    {
        var validator = new UpdateRiskHistoryEntryCommandValidator();
        ValidationResult result = validator.Validate(new UpdateRiskHistoryEntryCommand(RiskId, Guid.Empty, "text"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void DeleteRiskHistoryEntry_WhenRiskIdEmpty_Fails()
    {
        var validator = new DeleteRiskHistoryEntryCommandValidator();
        ValidationResult result = validator.Validate(new DeleteRiskHistoryEntryCommand(Guid.Empty, EntryId));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetRiskTeamMembers_WhenRiskIdEmpty_Fails()
    {
        var validator = new SetRiskTeamMembersCommandValidator();
        ValidationResult result = validator.Validate(new SetRiskTeamMembersCommand(Guid.Empty, []));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetRiskTeamMembers_WhenValid_Passes()
    {
        var validator = new SetRiskTeamMembersCommandValidator();
        ValidationResult result = validator.Validate(new SetRiskTeamMembersCommand(RiskId, [Guid.NewGuid()]));
        Assert.True(result.IsValid);
    }
}
