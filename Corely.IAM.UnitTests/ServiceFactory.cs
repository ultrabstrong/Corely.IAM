using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.UnitTests;

public class ServiceFactory : MockDbServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceFactory()
        : base(new ServiceCollection(), new ConfigurationManager())
    {
        AddIAMServices();
        _serviceProvider = ServiceCollection.BuildServiceProvider();
    }

    protected override ISecurityConfigurationProvider GetSecurityConfigurationProvider() =>
        new SecurityConfigurationProvider();

    protected override void AddLogging(ILoggingBuilder builder) =>
        builder.AddProvider(NullLoggerProvider.Instance);

    public T GetRequiredService<T>()
        where T : notnull => _serviceProvider.GetRequiredService<T>();
}
