using CleanArchitecture.SharedKernel.Abstractions;

namespace CleanArchitecture.Infrastructure.Time;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
