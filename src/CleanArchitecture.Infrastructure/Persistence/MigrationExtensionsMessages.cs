using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Persistence;

internal static partial class MigrationExtensionsMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Applying {PendingMigrationCount} pending migration(s): {PendingMigrationNames}")]
    public static partial void ApplyingPendingMigrations(ILogger logger, int pendingMigrationCount, string pendingMigrationNames);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Database is already up to date; no migrations were applied")]
    public static partial void NoPendingMigrations(ILogger logger);
}
