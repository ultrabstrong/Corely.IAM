using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Corely.IAM.Validators.FluentValidators;
using Corely.Security.Encryption.Factories;
using Corely.Security.Hashing.Factories;
using Corely.Security.PasswordValidation.Providers;
using Corely.Security.Signature.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Corely.IAM.UnitTests;

public class ServiceRegistrationExtensionsTests
{
    private readonly IConfiguration _configuration;
    private readonly ISecurityConfigurationProvider _securityConfigurationProvider;

    public ServiceRegistrationExtensionsTests()
    {
        _configuration = new ConfigurationManager();
        _securityConfigurationProvider = new SecurityConfigurationProvider();
    }

    private static IServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(NullLoggerProvider.Instance);
        });
        return services;
    }

    [Fact]
    public void AddIAMServicesWithMockDb_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServiceCollection();

        // Act
        var result = services.AddIAMServicesWithMockDb(
            _configuration,
            _securityConfigurationProvider
        );

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddIAMServicesWithEF_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServiceCollection();

        // Act
        var result = services.AddIAMServicesWithEF(_configuration, _securityConfigurationProvider);

        // Assert
        Assert.Same(services, result);
    }

    [Theory]
    [InlineData(typeof(IValidationProvider))]
    [InlineData(typeof(IFluentValidatorFactory))]
    [InlineData(typeof(ISymmetricEncryptionProviderFactory))]
    [InlineData(typeof(IAsymmetricEncryptionProviderFactory))]
    [InlineData(typeof(IAsymmetricSignatureProviderFactory))]
    [InlineData(typeof(IHashProviderFactory))]
    [InlineData(typeof(ISecurityProvider))]
    [InlineData(typeof(IPasswordValidationProvider))]
    [InlineData(typeof(ISecurityConfigurationProvider))]
    [InlineData(typeof(IAuthenticationProvider))]
    [InlineData(typeof(IUserContextProvider))]
    [InlineData(typeof(IUserContextSetter))]
    [InlineData(typeof(IAuthorizationProvider))]
    [InlineData(typeof(IRegistrationService))]
    [InlineData(typeof(IDeregistrationService))]
    [InlineData(typeof(IAuthenticationService))]
    [InlineData(typeof(IUserOwnershipProcessor))]
    [InlineData(typeof(IAccountProcessor))]
    [InlineData(typeof(IUserProcessor))]
    [InlineData(typeof(IBasicAuthProcessor))]
    [InlineData(typeof(IGroupProcessor))]
    [InlineData(typeof(IRoleProcessor))]
    [InlineData(typeof(IPermissionProcessor))]
    public void AddIAMServicesWithMockDb_RegistersService(Type serviceType)
    {
        // Arrange
        var services = CreateServiceCollection();

        // Act
        services.AddIAMServicesWithMockDb(_configuration, _securityConfigurationProvider);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService(serviceType);
        Assert.NotNull(service);
    }

    [Theory]
    [InlineData(typeof(IValidationProvider))]
    [InlineData(typeof(IFluentValidatorFactory))]
    [InlineData(typeof(ISymmetricEncryptionProviderFactory))]
    [InlineData(typeof(IAsymmetricEncryptionProviderFactory))]
    [InlineData(typeof(IAsymmetricSignatureProviderFactory))]
    [InlineData(typeof(IHashProviderFactory))]
    [InlineData(typeof(ISecurityProvider))]
    [InlineData(typeof(IPasswordValidationProvider))]
    [InlineData(typeof(ISecurityConfigurationProvider))]
    [InlineData(typeof(IAuthenticationProvider))]
    [InlineData(typeof(IUserContextProvider))]
    [InlineData(typeof(IUserContextSetter))]
    [InlineData(typeof(IAuthorizationProvider))]
    [InlineData(typeof(IRegistrationService))]
    [InlineData(typeof(IDeregistrationService))]
    [InlineData(typeof(IAuthenticationService))]
    [InlineData(typeof(IUserOwnershipProcessor))]
    [InlineData(typeof(IAccountProcessor))]
    [InlineData(typeof(IUserProcessor))]
    [InlineData(typeof(IBasicAuthProcessor))]
    [InlineData(typeof(IGroupProcessor))]
    [InlineData(typeof(IRoleProcessor))]
    [InlineData(typeof(IPermissionProcessor))]
    public void AddIAMServicesWithEF_RegistersService(Type serviceType)
    {
        // Arrange
        var services = CreateServiceCollection();

        // Act
        services.AddIAMServicesWithEF(_configuration, _securityConfigurationProvider);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService(serviceType);
        Assert.NotNull(service);
    }

    [Fact]
    public void AddIAMServicesWithMockDb_UserContextProvider_ReturnsSameInstanceForBothInterfaces()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddIAMServicesWithMockDb(_configuration, _securityConfigurationProvider);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope = serviceProvider.CreateScope();
        var userContextProvider = scope.ServiceProvider.GetRequiredService<IUserContextProvider>();
        var userContextSetter = scope.ServiceProvider.GetRequiredService<IUserContextSetter>();

        // Assert
        Assert.Same(userContextProvider, userContextSetter);
    }

    [Theory]
    [InlineData(typeof(ISymmetricEncryptionProviderFactory))]
    [InlineData(typeof(IAsymmetricEncryptionProviderFactory))]
    [InlineData(typeof(IAsymmetricSignatureProviderFactory))]
    [InlineData(typeof(IHashProviderFactory))]
    [InlineData(typeof(ISecurityProvider))]
    public void AddIAMServicesWithMockDb_SingletonServices_ReturnsSameInstance(Type serviceType)
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddIAMServicesWithMockDb(_configuration, _securityConfigurationProvider);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var instance1 = serviceProvider.GetService(serviceType);
        var instance2 = serviceProvider.GetService(serviceType);

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Theory]
    [InlineData(typeof(IValidationProvider))]
    [InlineData(typeof(IFluentValidatorFactory))]
    [InlineData(typeof(IPasswordValidationProvider))]
    [InlineData(typeof(IAuthenticationProvider))]
    [InlineData(typeof(IAuthorizationProvider))]
    [InlineData(typeof(IRegistrationService))]
    [InlineData(typeof(IDeregistrationService))]
    [InlineData(typeof(IAuthenticationService))]
    [InlineData(typeof(IAccountProcessor))]
    [InlineData(typeof(IUserProcessor))]
    [InlineData(typeof(IBasicAuthProcessor))]
    [InlineData(typeof(IGroupProcessor))]
    [InlineData(typeof(IRoleProcessor))]
    [InlineData(typeof(IPermissionProcessor))]
    public void AddIAMServicesWithMockDb_ScopedServices_ReturnsDifferentInstancePerScope(
        Type serviceType
    )
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddIAMServicesWithMockDb(_configuration, _securityConfigurationProvider);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        object instance1;
        object instance2;
        using (var scope1 = serviceProvider.CreateScope())
        {
            instance1 = scope1.ServiceProvider.GetService(serviceType)!;
        }
        using (var scope2 = serviceProvider.CreateScope())
        {
            instance2 = scope2.ServiceProvider.GetService(serviceType)!;
        }

        // Assert
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void AddIAMServicesWithMockDb_CanBuildServiceProvider()
    {
        // Arrange
        var services = CreateServiceCollection();

        // Act
        services.AddIAMServicesWithMockDb(_configuration, _securityConfigurationProvider);
        var exception = Record.Exception(() => services.BuildServiceProvider());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void AddIAMServicesWithEF_CanBuildServiceProvider()
    {
        // Arrange
        var services = CreateServiceCollection();

        // Act
        services.AddIAMServicesWithEF(_configuration, _securityConfigurationProvider);
        var exception = Record.Exception(() => services.BuildServiceProvider());

        // Assert
        Assert.Null(exception);
    }
}
