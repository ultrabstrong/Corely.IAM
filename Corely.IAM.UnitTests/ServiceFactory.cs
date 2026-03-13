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

        var options = IAMOptions.Create(
            new ConfigurationManager(),
            new SecurityConfigurationProvider()
        );

        services.AddIAMServices(options);

        _serviceProvider = services.BuildServiceProvider();
    }

    public T GetRequiredService<T>()
        where T : notnull => _serviceProvider.GetRequiredService<T>();
}
