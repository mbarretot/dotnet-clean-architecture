using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CleanArchitecture.Infrastructure.Persistence;

internal sealed class DatabaseFacadeMigrationRunner(DatabaseFacade database) : IMigrationRunner
{
    public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken) =>
        (await database.GetPendingMigrationsAsync(cancellationToken)).ToList();

    public Task MigrateAsync(CancellationToken cancellationToken) => database.MigrateAsync(cancellationToken);
}
