namespace CleanArchitecture.SharedKernel.Entities;

public interface ISoftDeletable
{
    bool IsDeleted { get; }

    DateTimeOffset? DeletedOnUtc { get; }

    string? DeletedBy { get; }
}
