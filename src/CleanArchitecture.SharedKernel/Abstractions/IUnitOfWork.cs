namespace CleanArchitecture.SharedKernel.Abstractions;

/// <summary>Defined here (not Infrastructure) so Application handlers can depend on it without that reference.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
