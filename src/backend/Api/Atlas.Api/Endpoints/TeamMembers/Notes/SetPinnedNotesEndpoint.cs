using Atlas.Api.DTOs.TeamMembers.Notes;
using Atlas.Application.Features.TeamMembers.Notes.SetPinnedNotes;

namespace Atlas.Api.Endpoints.TeamMembers.Notes;

public sealed class SetPinnedNotesEndpoint : Endpoint<SetPinnedNotesRequest>
{
    private readonly IMediator _mediator;

    public SetPinnedNotesEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/team-members/{teamMemberId:guid}/notes/pins");
        AllowAnonymous();
        Summary(s => { s.Summary = "Set pinned notes ordering"; });
    }

    public override async Task HandleAsync(SetPinnedNotesRequest req, CancellationToken ct)
    {
        var teamMemberId = Route<Guid>("teamMemberId");
        req = req with { TeamMemberId = teamMemberId };

        var ok = await _mediator.Send(new SetPinnedNotesCommand(req.TeamMemberId, req.NoteIdsInOrder), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

