namespace CleanArchitecture.SharedKernel.Entities;

/// <summary>Compared by <see cref="Id"/>, not by state.</summary>
public abstract class BaseEntity : IAuditable, IEquatable<BaseEntity>
{
    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    protected BaseEntity()
        : this(Guid.NewGuid())
    {
    }

    public Guid Id { get; protected init; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static bool operator ==(BaseEntity? left, BaseEntity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(BaseEntity? left, BaseEntity? right) => !(left == right);

    public bool Equals(BaseEntity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as BaseEntity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
