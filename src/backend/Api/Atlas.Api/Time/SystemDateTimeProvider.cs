using Atlas.Application.Abstractions.Time;

namespace Atlas.Api.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
