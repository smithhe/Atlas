using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Growth.Skills.SetGrowthSkillsInProgress;

public sealed class SetGrowthSkillsInProgressCommandHandler : IRequestHandler<SetGrowthSkillsInProgressCommand, bool>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public SetGrowthSkillsInProgressCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<bool> Handle(SetGrowthSkillsInProgressCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var plan = await _growth.GetByIdWithDetailsAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        // Match UI semantics:
        // - trim + drop blanks
        // - keep order
        // - remove duplicates (case-sensitive, first occurrence wins)
        var next = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in request.SkillsInProgress)
        {
            var value = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!seen.Add(value)) continue;
            next.Add(value);
        }

        // Remove rows that are no longer present (by exact value).
        plan.SkillsInProgress.RemoveAll(x => !next.Contains(x.Value, StringComparer.Ordinal));

        // Add missing rows and update sort order for kept rows.
        for (var i = 0; i < next.Count; i++)
        {
            var value = next[i];
            var existing = plan.SkillsInProgress.FirstOrDefault(x => string.Equals(x.Value, value, StringComparison.Ordinal));
            if (existing is null)
            {
                plan.SkillsInProgress.Add(new GrowthSkillInProgress
                {
                    GrowthId = plan.Id,
                    Value = value,
                    SortOrder = i
                });
                continue;
            }

            existing.SortOrder = i;
        }

        // Keep in-memory order stable after mutation.
        plan.SkillsInProgress = plan.SkillsInProgress
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Value, StringComparer.Ordinal)
            .ToList();

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

