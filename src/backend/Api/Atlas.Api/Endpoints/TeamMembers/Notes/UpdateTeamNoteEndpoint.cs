using Atlas.Api.DTOs.TeamMembers.Notes;
using Atlas.Application.Features.TeamMembers.Notes.UpdateTeamNote;

namespace Atlas.Api.Endpoints.TeamMembers.Notes;

public sealed class UpdateTeamNoteEndpoint : Endpoint<UpdateTeamNoteRequest>
{
    private readonly IMediator _mediator;

    public UpdateTeamNoteEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/team-members/{teamMemberId:guid}/notes/{noteId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update a team note"; });
    }

    public override async Task HandleAsync(UpdateTeamNoteRequest req, CancellationToken ct)
    {
        var teamMemberId = Route<Guid>("teamMemberId");
        var noteId = Route<Guid>("noteId");
        req = req with { TeamMemberId = teamMemberId, NoteId = noteId };

        var ok = await _mediator.Send(new UpdateTeamNoteCommand(req.TeamMemberId, req.NoteId, req.Type, req.Title, req.Text, req.PinnedOrder), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

