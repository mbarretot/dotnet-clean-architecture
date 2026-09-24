using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CleanArchitecture.Infrastructure.UnitTests.Persistence;

public sealed class MigrationExtensionsTests
{
    private readonly IMigrationRunner _migrationRunner = Substitute.For<IMigrationRunner>();

    [Fact]
    public async Task ApplyPendingMigrationsAsync_WhenMigrationsArePending_AppliesThem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _migrationRunner.GetPendingMigrationsAsync(cancellationToken).Returns(["20260922031520_InitialCreate"]);

        await MigrationExtensions.ApplyPendingMigrationsAsync(_migrationRunner, NullLogger.Instance, cancellationToken);

        await _migrationRunner.Received(1).MigrateAsync(cancellationToken);
    }

    [Fact]
    public async Task ApplyPendingMigrationsAsync_WhenNoMigrationsArePending_DoesNotApplyAnything()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _migrationRunner.GetPendingMigrationsAsync(cancellationToken).Returns([]);

        await MigrationExtensions.ApplyPendingMigrationsAsync(_migrationRunner, NullLogger.Instance, cancellationToken);

        await _migrationRunner.DidNotReceive().MigrateAsync(Arg.Any<CancellationToken>());
    }
}
