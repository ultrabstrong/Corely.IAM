using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.DataAccess.Mock;
using Corely.DataAccess.Mock.Repos;
using Corely.Security.KeyStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.UnitTests;

public class ServiceFactoryBaseTests : ServiceFactoryGenericTests
{
    private class MockServiceFactory(
        IServiceCollection serviceCollection,
        IConfiguration configuration
    ) : ServiceFactoryBase(serviceCollection, configuration)
    {
        private class MockSecurityConfigurationProvider : ISecurityConfigurationProvider
        {
            public ISymmetricKeyStoreProvider GetSystemSymmetricKey() => null!;
        }

        protected override ISecurityConfigurationProvider GetSecurityConfigurationProvider() =>
            new MockSecurityConfigurationProvider();

        protected override void AddLogging(ILoggingBuilder builder) =>
            builder.AddProvider(NullLoggerProvider.Instance);

        protected override void AddDataServices()
        {
            ServiceCollection.AddSingleton(typeof(IReadonlyRepo<>), typeof(MockReadonlyRepo<>));
            ServiceCollection.AddSingleton(typeof(IRepo<>), typeof(MockRepo<>));
            ServiceCollection.AddSingleton<IUnitOfWorkProvider, MockUoWProvider>();
        }
    }

    protected override ServiceFactoryBase CreateServiceFactory(
        IServiceCollection serviceCollection,
        IConfiguration configuration
    )
    {
        return new MockServiceFactory(serviceCollection, configuration);
    }
}
