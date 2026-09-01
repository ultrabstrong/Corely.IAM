using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Corely.IAM.IntegrationTests.Infrastructure;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MsSql;
using Testcontainers.MySql;

namespace Corely.IAM.IntegrationTests.Providers;

public sealed class ProviderTestHost(DatabaseProvider provider) : IAsyncLifetime
{
    private IContainer? _container;
    private ServiceProvider? _serviceProvider;

    public TestTimeProvider TimeProvider { get; } =
        new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    public DatabaseProvider Provider => provider;

    public async ValueTask InitializeAsync()
    {
        var connectionString = await StartContainerAsync();

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
                }
            )
            .Build();

        services.AddIAMServices(
            IAMOptions.Create(
                configuration,
                new TestSecurityConfigurationProvider(),
                _ => CreateEFConfiguration(connectionString)
            )
        );

        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(TimeProvider);

        _serviceProvider = services.BuildServiceProvider();
    }

    private async Task<string> StartContainerAsync()
    {
        switch (provider)
        {
            case DatabaseProvider.MsSql:
                var mssql = new MsSqlBuilder().Build();
                _container = mssql;
                await mssql.StartAsync();
                return mssql.GetConnectionString();

            case DatabaseProvider.MySql:
                var mysql = new MySqlBuilder().WithDatabase("corely_iam").Build();
                _container = mysql;
                await mysql.StartAsync();
                return mysql.GetConnectionString();

            default:
                throw new NotSupportedException($"Unsupported provider {provider}.");
        }
    }

    private IEFConfiguration CreateEFConfiguration(string connectionString) =>
        provider switch
        {
            DatabaseProvider.MsSql => new TestMsSqlConfiguration(connectionString),
            DatabaseProvider.MySql => new TestMySqlConfiguration(
                connectionString,
                TestMySqlConfiguration.MYSQL_MIGRATIONS_ASSEMBLY
            ),
            _ => throw new NotSupportedException($"Unsupported provider {provider}."),
        };

    public Task MigrateAsync() =>
        WithScopeAsync(services =>
            services.GetRequiredService<IamDbContext>().Database.MigrateAsync()
        );

    public async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        using var scope = _serviceProvider!.CreateScope();
        return await work(scope.ServiceProvider);
    }

    public async Task WithScopeAsync(Func<IServiceProvider, Task> work)
    {
        using var scope = _serviceProvider!.CreateScope();
        await work(scope.ServiceProvider);
    }

    internal Task<T> QueryAsync<T>(Func<IamDbContext, Task<T>> query) =>
        WithScopeAsync(services => query(services.GetRequiredService<IamDbContext>()));

    public async ValueTask DisposeAsync()
    {
        _serviceProvider?.Dispose();
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
