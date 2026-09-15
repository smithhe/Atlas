using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services
{

    /// <summary>Team note mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
    public sealed class TeamNoteService : ITeamNoteService
    {
        private readonly IAtlasApiClient _api;
        private readonly IAppCacheService _cache;

        public TeamNoteService(IAtlasApiClient api, IAppCacheService cache)
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

            if (res.Id is not Guid noteId || noteId == Guid.Empty)
            {
                throw new InvalidOperationException("Add team note response did not include a note id.");
            }

            var now = DateTimeOffset.UtcNow.ToString("o");
            var note = new TeamNote
            {
                Id = noteId,
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

            var nextNotes = member.Notes.Select(x => x.Id == note.Id ? note : x).ToList();
            TeamMember nextMember = EntityClone.TeamMember(member, notes: nextNotes);
            await OptimisticCache.ApplyAsync(
                member,
                nextMember,
                m => EntityClone.TeamMember(m),
                _cache.UpdateTeamMember,
                () => _api.AtlasApiEndpointsTeamMembersNotesUpdateTeamNoteEndpointAsync(
                    memberId,
                    note.Id,
                    EntityRequestMappers.ToUpdateTeamNoteRequest(note),
                    cancellationToken));
        }
    }
}
