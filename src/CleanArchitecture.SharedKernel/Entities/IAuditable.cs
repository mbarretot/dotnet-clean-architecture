namespace CleanArchitecture.SharedKernel.Entities;

public interface IAuditable
{
    string CreatedBy { get; }

    DateTimeOffset CreatedAt { get; }

    string? ModifiedBy { get; }

    DateTimeOffset? ModifiedAt { get; }
}
