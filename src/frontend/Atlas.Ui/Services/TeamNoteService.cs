using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Team note mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class TeamNoteService
{
    private readonly IAtlasApiClient _api;
    private readonly AppCacheService _cache;

    public TeamNoteService(IAtlasApiClient api, AppCacheService cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<TeamNote> AddAsync(
        Guid memberId,
        NoteTag tag,
        string text,
        string? title,
        string? adoWorkItemId,
        string? prUrl,
        CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsTeamMembersNotesAddTeamNoteResponse res =
            await _api.AtlasApiEndpointsTeamMembersNotesAddTeamNoteEndpointAsync(
                memberId,
                EntityRequestMappers.ToAddTeamNoteRequest(tag, text, title, adoWorkItemId, prUrl),
                cancellationToken);

        var now = DateTimeOffset.UtcNow.ToString("o");
        var note = new TeamNote
        {
            Id = res.Id ?? Guid.NewGuid(),
            CreatedIso = now,
            LastModifiedIso = now,
            Tag = tag,
            Title = title,
            Text = text,
            AdoWorkItemId = adoWorkItemId,
            PrUrl = prUrl
        };

        TeamMember? member = _cache.Team.FirstOrDefault(m => m.Id == memberId);
        if (member is not null)
        {
            _cache.UpdateTeamMember(EntityClone.TeamMember(
                member,
                notes: new[] { note }.Concat(member.Notes).ToList()));
        }

        return note;
    }

    public async Task UpdateAsync(Guid memberId, TeamNote note, CancellationToken cancellationToken = default)
    {
        TeamMember? member = _cache.Team.FirstOrDefault(m => m.Id == memberId);
        if (member is null)
        {
            await _api.AtlasApiEndpointsTeamMembersNotesUpdateTeamNoteEndpointAsync(
                memberId,
                note.Id,
                EntityRequestMappers.ToUpdateTeamNoteRequest(note),
                cancellationToken);
            return;
        }

        TeamMember previousClone = EntityClone.TeamMember(member);
        var nextNotes = member.Notes.Select(x => x.Id == note.Id ? note : x).ToList();
        _cache.UpdateTeamMember(EntityClone.TeamMember(member, notes: nextNotes));
        try
        {
            await _api.AtlasApiEndpointsTeamMembersNotesUpdateTeamNoteEndpointAsync(
                memberId,
                note.Id,
                EntityRequestMappers.ToUpdateTeamNoteRequest(note),
                cancellationToken);
        }
        catch
        {
            _cache.UpdateTeamMember(previousClone);
            throw;
        }
    }
}
