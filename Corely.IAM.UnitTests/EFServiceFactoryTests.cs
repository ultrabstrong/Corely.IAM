using AutoFixture;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.Security.KeyStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.UnitTests;

public class EFServiceFactoryTests : ServiceFactoryGenericTests
{
    private class TestEFConfiguration : EFInMemoryConfigurationBase
    {
        public override void Configure(DbContextOptionsBuilder optionsBuilder)
        {
            var fixture = new Fixture();
            optionsBuilder.UseInMemoryDatabase(fixture.Create<string>());
        }
    }

    private class MockServiceFactory(
        IServiceCollection serviceCollection,
        IConfiguration configuration
    ) : EFServiceFactory(serviceCollection, configuration)
    {
        private class MockSecurityConfigurationProvider : ISecurityConfigurationProvider
        {
            public ISymmetricKeyStoreProvider GetSystemSymmetricKey() => null!;
        }

        protected override ISecurityConfigurationProvider GetSecurityConfigurationProvider() =>
            new MockSecurityConfigurationProvider();

        protected override void AddLogging(ILoggingBuilder builder) =>
            builder.AddProvider(NullLoggerProvider.Instance);

        protected override IEFConfiguration GetEFConfiguration(IServiceProvider sp) =>
            new TestEFConfiguration();
    }

    protected override ServiceFactoryBase CreateServiceFactory(
        IServiceCollection serviceCollection,
        IConfiguration configuration
    )
    {
        return new MockServiceFactory(serviceCollection, configuration);
    }
}
