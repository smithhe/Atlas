using Atlas.Application.Features.Tasks.CreateTask;
using Atlas.Api.DTOs.Tasks;
using Atlas.Domain.Enums;

namespace Atlas.Api.Endpoints.Tasks;

public sealed class CreateTaskEndpoint : Endpoint<CreateTaskRequest, CreateTaskResponse>
{
    private readonly IMediator _mediator;

    public CreateTaskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/tasks");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a task";
        });
    }

    public override async Task HandleAsync(CreateTaskRequest req, CancellationToken ct)
    {
        try
        {
            var id = await _mediator.Send(new CreateTaskCommand(
                Title: req.Title,
                Priority: req.Priority,
                Status: req.Status,
                AssigneeId: req.AssigneeId,
                ProjectId: req.ProjectId,
                RiskId: req.RiskId,
                DueDate: req.DueDate,
                EstimatedDurationText: req.EstimatedDurationText,
                EstimateConfidence: req.EstimateConfidence,
                ActualDurationText: req.ActualDurationText,
                Notes: req.Notes,
                BlockedByTaskIds: req.DependencyTaskIds), ct);

            if (id == Guid.Empty)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.ResponseAsync(new CreateTaskResponse(id), 201, ct);
        }
        catch (InvalidOperationException ex)
        {
            // e.g. blocked-by task was not found or dependency cycle detected.
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}

