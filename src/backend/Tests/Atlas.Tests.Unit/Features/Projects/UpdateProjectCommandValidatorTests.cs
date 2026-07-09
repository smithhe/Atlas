using Atlas.Application.Features.Projects.UpdateProject;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Projects;

public sealed class UpdateProjectCommandValidatorTests
{
    private readonly UpdateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateProjectCommand.Id));
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Name = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateProjectCommand.Name));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static UpdateProjectCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Name: "Atlas",
        Summary: "Updated summary",
        Description: null,
        Status: ProjectStatus.Active,
        Health: HealthSignal.Yellow,
        TargetDate: null,
        Priority: Priority.Medium,
        ProductOwnerId: null,
        Tags: null,
        Links: null);
}
