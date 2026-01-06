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
         // - remove duplicates (case-insensitive, first occurrence wins)
        var next = new List<string>();
         var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in request.SkillsInProgress)
        {
            var value = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!seen.Add(value)) continue;
            next.Add(value);
        }

         // Remove rows that are no longer present (case-insensitive).
         plan.SkillsInProgress.RemoveAll(x => !next.Any(v => string.Equals(v, x.Value, StringComparison.OrdinalIgnoreCase)));

         // Add missing rows, de-dupe any existing casing-variants, and update sort order.
        for (var i = 0; i < next.Count; i++)
        {
            var value = next[i];
             var matches = plan.SkillsInProgress
                 .Where(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase))
                 .ToList();

             if (matches.Count == 0)
             {
                 plan.SkillsInProgress.Add(new GrowthSkillInProgress
                 {
                     GrowthId = plan.Id,
                     Value = value,
                     SortOrder = i
                 });
                 continue;
             }

             var keep = matches[0];
             keep.SortOrder = i;

             // If we somehow had duplicates with different casing (e.g., case-insensitive DB collation),
             // keep the first and remove the rest.
             for (var m = 1; m < matches.Count; m++)
             {
                 plan.SkillsInProgress.Remove(matches[m]);
             }
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

