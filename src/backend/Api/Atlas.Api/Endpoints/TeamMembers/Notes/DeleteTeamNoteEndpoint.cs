using Atlas.Api.DTOs.TeamMembers.Notes;
using Atlas.Application.Features.TeamMembers.Notes.DeleteTeamNote;

namespace Atlas.Api.Endpoints.TeamMembers.Notes;

public sealed class DeleteTeamNoteEndpoint : Endpoint<DeleteTeamNoteRequest>
{
    private readonly IMediator _mediator;

    public DeleteTeamNoteEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/team-members/{teamMemberId:guid}/notes/{noteId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Delete a team note"; });
    }

    public override async Task HandleAsync(DeleteTeamNoteRequest req, CancellationToken ct)
    {
        var teamMemberId = Route<Guid>("teamMemberId");
        var noteId = Route<Guid>("noteId");
        req = req with { TeamMemberId = teamMemberId, NoteId = noteId };

        var ok = await _mediator.Send(new DeleteTeamNoteCommand(req.TeamMemberId, req.NoteId), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

