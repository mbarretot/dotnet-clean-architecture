namespace CleanArchitecture.SharedKernel.Abstractions;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
