using Atlas.Application.DTOs;
using Atlas.Application.Features.Projects.CreateProject;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Projects;

public sealed class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Name = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateProjectCommand.Name));
    }

    [Fact]
    public void Validate_WhenSummaryIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Summary = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateProjectCommand.Summary));
    }

    [Fact]
    public void Validate_WhenLinkUrlIsInvalid_HasValidationError()
    {
        CreateProjectCommand command = ValidCommand() with
        {
            Links = [new ProjectLinkDto("Docs", "not-a-valid-uri")]
        };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("valid absolute URI"));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static CreateProjectCommand ValidCommand() => new(
        Name: "Atlas",
        Summary: "Delivery platform",
        Description: null,
        Status: ProjectStatus.Active,
        Health: HealthSignal.Green,
        TargetDate: null,
        Priority: Priority.Medium,
        ProductOwnerId: null,
        Tags: ["platform"],
        Links: [new ProjectLinkDto("Wiki", "https://example.com/wiki")]);
}
