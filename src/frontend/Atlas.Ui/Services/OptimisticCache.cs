namespace Atlas.Ui.Services;

public static class OptimisticCache
{
    public static async Task ApplyAsync<T>(
        T? previous,
        T next,
        Func<T, T> clone,
        Action<T> apply,
        Func<Task> http) where T : class
    {
        T? rollback = previous is not null ? clone(previous) : null;
        apply(next);
        try
        {
            await http().ConfigureAwait(false);
        }
        catch
        {
            if (rollback is not null)
            {
                apply(rollback);
            }

            throw;
        }
    }

    public static async Task PatchAsync<T>(
        T? current,
        Func<T, T> clone,
        Func<T, T> mutator,
        Action<T> apply,
        Func<Task> http) where T : class
    {
        if (current is null)
        {
            await http().ConfigureAwait(false);
            return;
        }

        T rollback = clone(current);
        apply(mutator(current));
        try
        {
            await http().ConfigureAwait(false);
        }
        catch
        {
            apply(rollback);
            throw;
        }
    }
}
