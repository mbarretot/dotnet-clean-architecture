namespace CleanArchitecture.SharedKernel.Entities;

/// <summary>Marks an entity that is flagged as deleted instead of being removed; persistence hides it from queries by default.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }

    DateTimeOffset? DeletedOnUtc { get; }

    string? DeletedBy { get; }
}
