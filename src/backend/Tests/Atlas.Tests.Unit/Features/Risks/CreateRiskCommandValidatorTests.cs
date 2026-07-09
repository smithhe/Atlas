using Atlas.Application.Features.Risks.CreateRisk;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Risks;

public sealed class CreateRiskCommandValidatorTests
{
    private readonly CreateRiskCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTitleIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Title = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateRiskCommand.Title));
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Description = null! });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateRiskCommand.Description));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static CreateRiskCommand ValidCommand() => new(
        Title: "Delivery slip",
        Status: RiskStatus.Open,
        Severity: SeverityLevel.High,
        ProjectId: null,
        Description: "Schedule risk",
        Evidence: "Velocity dropped");
}
