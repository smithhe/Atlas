using Atlas.Application.Features.Tasks.CreateTask;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Tasks;

public sealed class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTitleIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Title = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskCommand.Title));
    }

    [Fact]
    public void Validate_WhenNotesIsNull_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Notes = null! });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskCommand.Notes));
    }

    [Fact]
    public void Validate_WhenEstimatedDurationTextIsNull_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { EstimatedDurationText = null! });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskCommand.EstimatedDurationText));
    }

    [Fact]
    public void Validate_WhenBlockedByTaskIdsContainsEmptyGuid_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { BlockedByTaskIds = [Guid.Empty] });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.StartsWith(nameof(CreateTaskCommand.BlockedByTaskIds)));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static CreateTaskCommand ValidCommand() => new(
        Title: "Ship feature",
        Priority: Priority.Medium,
        Status: Domain.Enums.TaskStatus.NotStarted,
        AssigneeId: null,
        ProjectId: null,
        RiskId: null,
        DueDate: null,
        EstimatedDurationText: "1h",
        EstimateConfidence: Confidence.Medium,
        ActualDurationText: null,
        Notes: string.Empty);
}
