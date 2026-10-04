using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Application.UnitTests.TestDoubles;

/// <summary>A real in-memory <see cref="HybridCache"/>; each call returns a fresh, empty instance.</summary>
internal static class TestHybridCache
{
    public static HybridCache Create()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();

        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }
}
