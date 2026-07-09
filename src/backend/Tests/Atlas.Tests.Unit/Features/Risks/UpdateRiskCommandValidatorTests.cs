using Atlas.Application.Features.Risks.UpdateRisk;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Risks;

public sealed class UpdateRiskCommandValidatorTests
{
    private readonly UpdateRiskCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateRiskCommand.Id));
    }

    [Fact]
    public void Validate_WhenEvidenceIsNull_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Evidence = null! });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateRiskCommand.Evidence));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static UpdateRiskCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Title: "Updated risk",
        Status: RiskStatus.Watching,
        Severity: SeverityLevel.Medium,
        ProjectId: null,
        Description: "Watching delivery",
        Evidence: "Mitigation in place");
}
