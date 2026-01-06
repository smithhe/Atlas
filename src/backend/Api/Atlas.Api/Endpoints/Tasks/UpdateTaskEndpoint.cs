using Atlas.Application.Features.Tasks.UpdateTask;
using Atlas.Api.DTOs.Tasks;
using Atlas.Domain.Enums;

namespace Atlas.Api.Endpoints.Tasks;

public sealed class UpdateTaskEndpoint : Endpoint<UpdateTaskRequest>
{
    private readonly IMediator _mediator;

    public UpdateTaskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/tasks/{id:guid}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update a task";
        });
    }

    public override async Task HandleAsync(UpdateTaskRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");

        try
        {
            var ok = await _mediator.Send(new UpdateTaskCommand(
                Id: id,
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

            if (!ok)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}

