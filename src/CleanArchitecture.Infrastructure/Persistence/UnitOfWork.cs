using CleanArchitecture.SharedKernel.Abstractions;

namespace CleanArchitecture.Infrastructure.Persistence;

/// <summary>Auditing and domain-event dispatch happen inside this call, via the registered interceptors.</summary>
public sealed class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
