using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Team note mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class TeamNoteService
{
    private readonly IAtlasApiClient _api;

    public TeamNoteService(IAtlasApiClient api)
    {
        _api = api;
    }

    public Task<AtlasApiDTOsTeamMembersNotesAddTeamNoteResponse> AddAsync(
        Guid memberId,
        NoteTag tag,
        string text,
        string? title,
        string? adoWorkItemId,
        string? prUrl,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsTeamMembersNotesAddTeamNoteEndpointAsync(memberId, new AtlasApiDTOsTeamMembersNotesAddTeamNoteRequest
        {
            Type = ApiMappers.ToApiNoteType(tag),
            Title = title,
            Text = text,
            AdoWorkItemId = adoWorkItemId,
            PrUrl = prUrl
        }, cancellationToken);

    public Task UpdateAsync(Guid memberId, TeamNote note, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsTeamMembersNotesUpdateTeamNoteEndpointAsync(memberId, note.Id, new AtlasApiDTOsTeamMembersNotesUpdateTeamNoteRequest
        {
            Type = ApiMappers.ToApiNoteType(note.Tag),
            Title = note.Title,
            Text = note.Text,
            AdoWorkItemId = note.AdoWorkItemId,
            PrUrl = note.PrUrl
        }, cancellationToken);
}
