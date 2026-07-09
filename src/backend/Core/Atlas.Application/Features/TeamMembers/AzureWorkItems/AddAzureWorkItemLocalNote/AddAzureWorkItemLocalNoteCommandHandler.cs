using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.TeamMembers.AzureWorkItems.AddAzureWorkItemLocalNote;

public sealed class AddAzureWorkItemLocalNoteCommandHandler : IRequestHandler<AddAzureWorkItemLocalNoteCommand, Guid>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public AddAzureWorkItemLocalNoteCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddAzureWorkItemLocalNoteCommand request, CancellationToken cancellationToken)
    {
        await using IUnitOfWorkTransaction tx = await _uow.BeginTransactionAsync(cancellationToken);

        TeamMember? member = await _team.GetByIdWithDetailsAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Guid.Empty;
        }

        var linked = member.AzureWorkItemLinks.Any(x => x.AzureWorkItem?.WorkItemId == request.WorkItemId);
        if (!linked)
        {
            await tx.RollbackAsync(cancellationToken);
            return Guid.Empty;
        }

        var note = new AzureWorkItemLocalNote
        {
            Id = Guid.NewGuid(),
            TeamMemberId = member.Id,
            WorkItemId = request.WorkItemId,
            CreatedAt = DateTimeOffset.UtcNow,
            Text = request.Text,
        };

        // Insert via DbSet (same pattern as notes/risks) so InMemory EF does not
        // treat the heavily-included aggregate graph as a concurrent update.
        await _team.AddAzureWorkItemLocalNoteAsync(note, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return note.Id;
    }
}
