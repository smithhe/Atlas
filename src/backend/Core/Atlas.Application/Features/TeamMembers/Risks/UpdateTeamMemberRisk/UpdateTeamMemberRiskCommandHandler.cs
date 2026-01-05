using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.TeamMembers.Risks.UpdateTeamMemberRisk;

public sealed class UpdateTeamMemberRiskCommandHandler : IRequestHandler<UpdateTeamMemberRiskCommand, bool>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public UpdateTeamMemberRiskCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateTeamMemberRiskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = await _team.GetByIdWithDetailsAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var risk = member.Risks.FirstOrDefault(r => r.Id == request.TeamMemberRiskId);
        if (risk is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        risk.Title = request.Title;
        risk.Severity = request.Severity;
        risk.RiskType = request.RiskType;
        risk.Status = request.Status;
        risk.Trend = request.Trend;
        risk.FirstNoticedDate = request.FirstNoticedDate;
        risk.ImpactArea = request.ImpactArea;
        risk.Description = request.Description;
        risk.CurrentAction = request.CurrentAction;
        risk.LinkedGlobalRiskId = request.LinkedGlobalRiskId;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

