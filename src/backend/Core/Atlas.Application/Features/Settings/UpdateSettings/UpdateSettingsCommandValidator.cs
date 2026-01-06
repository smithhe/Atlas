namespace Atlas.Application.Features.Settings.UpdateSettings;

public sealed class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsCommandValidator()
    {
        RuleFor(x => x.StaleDays)
            .GreaterThan(0)
            .LessThanOrEqualTo(365);

        RuleFor(x => x.AzureDevOpsBaseUrl)
            .MaximumLength(500);
    }
}

