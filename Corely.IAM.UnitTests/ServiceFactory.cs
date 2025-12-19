using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.UnitTests;

public class ServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceFactory()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(NullLoggerProvider.Instance);
        });

        var securityConfigurationProvider = new SecurityConfigurationProvider();

        services.AddIAMServicesWithMockDb(
            new ConfigurationManager(),
            new SecurityConfigurationProvider()
        );

        _serviceProvider = services.BuildServiceProvider();
    }

    public T GetRequiredService<T>()
        where T : notnull => _serviceProvider.GetRequiredService<T>();
}
