using Atlas.Application.Features.Tasks.UpdateTask;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Tasks;

public sealed class UpdateTaskCommandValidatorTests
{
    private readonly UpdateTaskCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTaskCommand.Id));
    }

    [Fact]
    public void Validate_WhenTitleIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Title = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTaskCommand.Title));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static UpdateTaskCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Title: "Updated task",
        Priority: Priority.High,
        Status: Domain.Enums.TaskStatus.InProgress,
        AssigneeId: null,
        ProjectId: null,
        RiskId: null,
        DueDate: null,
        EstimatedDurationText: "2h",
        EstimateConfidence: Confidence.High,
        ActualDurationText: null,
        Notes: "notes",
        BlockedByTaskIds: null);
}
