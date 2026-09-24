using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Interceptors;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.UnitTests.Persistence.Interceptors;

public sealed class SqliteApplicationDbContextFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ApplicationDbContext CreateContext(IDateTimeProvider dateTimeProvider, ICurrentUser currentUser, IPublisher publisher)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new AuditableEntitySaveChangesInterceptor(dateTimeProvider, currentUser),
                new DispatchDomainEventsInterceptor(publisher))
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    public void Dispose() => _connection.Dispose();
}
