using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.TeamMembers.Risks.AddTeamMemberRisk;

public sealed class AddTeamMemberRiskCommandHandler : IRequestHandler<AddTeamMemberRiskCommand, Guid>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public AddTeamMemberRiskCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddTeamMemberRiskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = await _team.GetByIdWithDetailsAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Guid.Empty;
        }

        var risk = new TeamMemberRisk
        {
            Id = Guid.NewGuid(),
            TeamMemberId = member.Id,
            Title = request.Title,
            Severity = request.Severity,
            RiskType = request.RiskType,
            Status = request.Status,
            Trend = request.Trend,
            FirstNoticedDate = request.FirstNoticedDate,
            ImpactArea = request.ImpactArea,
            Description = request.Description,
            CurrentAction = request.CurrentAction,
            LinkedGlobalRiskId = request.LinkedGlobalRiskId
        };

        member.Risks.Add(risk);

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return risk.Id;
    }
}

