using Atlas.Api.DTOs.TeamMembers.Notes;
using Atlas.Application.Features.TeamMembers.Notes.AddTeamNote;

namespace Atlas.Api.Endpoints.TeamMembers.Notes;

public sealed class AddTeamNoteEndpoint : Endpoint<AddTeamNoteRequest, AddTeamNoteResponse>
{
    private readonly IMediator _mediator;

    public AddTeamNoteEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/team-members/{teamMemberId:guid}/notes");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Add a team note";
            s.Response<AddTeamNoteResponse>(201, "Created");
        });
    }

    public override async Task HandleAsync(AddTeamNoteRequest req, CancellationToken ct)
    {
        Guid teamMemberId = Route<Guid>("teamMemberId");
        req = req with { TeamMemberId = teamMemberId };

        Guid id = await _mediator.Send(
            new AddTeamNoteCommand(req.TeamMemberId, req.Type, req.Title, req.Text, req.AdoWorkItemId, req.PrUrl),
            ct);
        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new AddTeamNoteResponse(id), 201, ct);
    }
}

