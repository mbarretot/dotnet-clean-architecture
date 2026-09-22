namespace CleanArchitecture.SharedKernel.Abstractions;

/// <summary>Testable seam over the clock; avoids direct <see cref="DateTimeOffset.UtcNow"/> calls.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
