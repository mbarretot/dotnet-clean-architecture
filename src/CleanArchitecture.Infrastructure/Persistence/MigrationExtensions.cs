using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Persistence;

public static class MigrationExtensions
{
    public static async Task ApplyPendingMigrationsAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        await using var scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var migrationRunner = new DatabaseFacadeMigrationRunner(dbContext.Database);

        await ApplyPendingMigrationsAsync(migrationRunner, logger, cancellationToken);
    }

    internal static async Task ApplyPendingMigrationsAsync(IMigrationRunner migrationRunner, ILogger logger, CancellationToken cancellationToken)
    {
        var pendingMigrations = await migrationRunner.GetPendingMigrationsAsync(cancellationToken);

        if (pendingMigrations.Count > 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var pendingMigrationNames = string.Join(", ", pendingMigrations);
                MigrationExtensionsMessages.ApplyingPendingMigrations(logger, pendingMigrations.Count, pendingMigrationNames);
            }

            await migrationRunner.MigrateAsync(cancellationToken);
        }
        else
        {
            MigrationExtensionsMessages.NoPendingMigrations(logger);
        }
    }
}
