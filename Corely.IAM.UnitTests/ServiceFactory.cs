using Corely.Security.Hashing;
using Corely.Security.Hashing.Factories;
using Corely.Security.Hashing.Providers;
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

        UseFastPasswordHashing(_serviceProvider);
    }

    /// <summary>
    /// Swaps the production PBKDF2 work factor for a trivial one.
    ///
    /// The real default is 600,000 iterations, which is the point of the algorithm but costs
    /// roughly 200ms per hash - and this suite hashes passwords in hundreds of tests. What is
    /// under test here is behaviour, not cost; the work factor itself is asserted in
    /// Corely.Security's own tests.
    /// </summary>
    internal static void UseFastPasswordHashing(IServiceProvider serviceProvider) =>
        serviceProvider
            .GetRequiredService<IHashProviderFactory>()
            .UpdateProvider(HashConstants.PBKDF2_SHA256_CODE, new Pbkdf2HashProvider(1000));

    public T GetRequiredService<T>()
        where T : notnull => _serviceProvider.GetRequiredService<T>();
}
