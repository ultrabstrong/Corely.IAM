using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Corely.Security.Hashing;
using Corely.Security.Hashing.Factories;
using Corely.Security.Hashing.Providers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.IntegrationTests.Infrastructure;

/// <summary>
/// The real IAM service graph wired to a real relational database.
///
/// This is the whole point of the tier: every processor, decorator, repository, and entity
/// configuration is the production one, and queries are translated to SQL and executed rather
/// than interpreted in memory by a test double.
/// </summary>
public sealed class IamTestHost : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _serviceProvider;

    public TestTimeProvider TimeProvider { get; } =
        new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    public IamTestHost()
    {
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(NullLoggerProvider.Instance);
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["SecurityOptions:AuthTokenTtlSeconds"] = "3600",
                    ["SecurityOptions:AuthSessionTtlSeconds"] = "604800",
                    ["SecurityOptions:MaxLoginAttempts"] = "5",
                    ["SecurityOptions:MfaChallengeTimeoutSeconds"] = "300",
                }
            )
            .Build();

        var options = IAMOptions.Create(
            configuration,
            new TestSecurityConfigurationProvider(),
            _ => new SqliteEFConfiguration(_connection)
        );

        services.AddIAMServices(options);

        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(TimeProvider);

        _serviceProvider = services.BuildServiceProvider();

        // The production PBKDF2 work factor is 600,000 iterations - roughly 200ms per hash. Every
        // scenario here registers users and signs them in, so paying that would dominate the run.
        // Behaviour is what is under test; the work factor is asserted in Corely.Security.
        _serviceProvider
            .GetRequiredService<IHashProviderFactory>()
            .UpdateProvider(HashConstants.PBKDF2_SHA256_CODE, new Pbkdf2HashProvider(1000));

        CreateSchema();
    }

    /// <summary>
    /// Creating the schema from the EF model is itself a test: it proves the IAM entity
    /// configurations - including the M:M join entities and their NoAction delete behaviour -
    /// round-trip through a relational provider.
    /// </summary>
    private void CreateSchema()
    {
        using var scope = _serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IamDbContext>().Database.EnsureCreated();
    }

    /// <summary>
    /// Each call gets a fresh scope. This matters beyond hygiene: the authorization provider
    /// caches within a scope, so permission changes are only observed by a later scope - exactly
    /// as they would be across separate requests in a running application.
    /// </summary>
    public async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        using var scope = _serviceProvider.CreateScope();
        return await work(scope.ServiceProvider);
    }

    public async Task WithScopeAsync(Func<IServiceProvider, Task> work)
    {
        using var scope = _serviceProvider.CreateScope();
        await work(scope.ServiceProvider);
    }

    internal Task<T> QueryAsync<T>(Func<IamDbContext, Task<T>> query) =>
        WithScopeAsync(services => query(services.GetRequiredService<IamDbContext>()));

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _connection.Dispose();
    }
}
