using Atlas.Application.Features.Projects.TeamMembers.SetProjectTeamMembers;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Projects;

public sealed class ProjectRelationshipValidatorTests
{
    [Fact]
    public void SetProjectTeamMembers_WhenProjectIdEmpty_Fails()
    {
        var validator = new SetProjectTeamMembersCommandValidator();
        ValidationResult result = validator.Validate(new SetProjectTeamMembersCommand(Guid.Empty, []));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetProjectTeamMembers_WhenTeamMemberIdsNull_Fails()
    {
        var validator = new SetProjectTeamMembersCommandValidator();
        ValidationResult result = validator.Validate(new SetProjectTeamMembersCommand(Guid.NewGuid(), null!));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetProjectTeamMembers_WhenValid_Passes()
    {
        var validator = new SetProjectTeamMembersCommandValidator();
        ValidationResult result = validator.Validate(new SetProjectTeamMembersCommand(Guid.NewGuid(), [Guid.NewGuid()]));
        Assert.True(result.IsValid);
    }
}
