namespace CleanArchitecture.Infrastructure.Persistence;

internal interface IMigrationRunner
{
    Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken);

    Task MigrateAsync(CancellationToken cancellationToken);
}
