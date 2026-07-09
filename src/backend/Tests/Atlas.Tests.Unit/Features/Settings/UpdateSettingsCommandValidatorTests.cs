using Atlas.Application.Features.Settings.UpdateSettings;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Settings;

public sealed class UpdateSettingsCommandValidatorTests
{
    private readonly UpdateSettingsCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Validate_WhenStaleDaysOutOfRange_HasValidationError(int staleDays)
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { StaleDays = staleDays });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateSettingsCommand.StaleDays));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static UpdateSettingsCommand ValidCommand() => new(
        StaleDays: 14,
        DefaultAiManualOnly: true,
        Theme: Theme.Dark,
        AzureDevOpsBaseUrl: "https://dev.azure.com/org");
}
