using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Risks.TeamMembers.SetRiskTeamMembers;

public sealed class SetRiskTeamMembersCommandHandler : IRequestHandler<SetRiskTeamMembersCommand, bool>
{
    private readonly IRiskRepository _risks;
    private readonly ITeamMemberRepository _teamMembers;
    private readonly IUnitOfWork _uow;

    public SetRiskTeamMembersCommandHandler(IRiskRepository risks, ITeamMemberRepository teamMembers, IUnitOfWork uow)
    {
        _risks = risks;
        _teamMembers = teamMembers;
        _uow = uow;
    }

    public async Task<bool> Handle(SetRiskTeamMembersCommand request, CancellationToken cancellationToken)
    {
        await using IUnitOfWorkTransaction tx = await _uow.BeginTransactionAsync(cancellationToken);

        Risk? risk = await _risks.GetByIdWithDetailsAsync(request.RiskId, cancellationToken);
        if (risk is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var desiredIds = request.TeamMemberIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        foreach (Guid memberId in desiredIds)
        {
            var exists = await _teamMembers.ExistsAsync(memberId, cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException($"Team member '{memberId}' was not found.");
            }
        }

        risk.LinkedTeamMembers.RemoveAll(x => !desiredIds.Contains(x.TeamMemberId));

        foreach (Guid memberId in desiredIds)
        {
            var exists = risk.LinkedTeamMembers.Any(x => x.TeamMemberId == memberId);
            if (exists)
            {
                continue;
            }

            risk.LinkedTeamMembers.Add(new RiskTeamMember
            {
                RiskId = risk.Id,
                TeamMemberId = memberId
            });
        }

        risk.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}
