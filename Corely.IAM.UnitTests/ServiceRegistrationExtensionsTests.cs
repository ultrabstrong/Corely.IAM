using System;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Permissions.Providers;
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

namespace Corely.IAM.UnitTests;

public class ServiceRegistrationExtensionsTests
{
    private readonly IConfiguration _configuration;
    private readonly ISecurityConfigurationProvider _securityConfigurationProvider;
    private readonly IAMOptions _mockDbOptions;
    private readonly IAMOptions _efOptions;
    private readonly Func<IServiceProvider, IEFConfiguration> _efConfigurationFactory;

    public ServiceRegistrationExtensionsTests()
    {
        _configuration = new ConfigurationManager();
        _securityConfigurationProvider = new SecurityConfigurationProvider();
        _mockDbOptions = IAMOptions.Create(_configuration, _securityConfigurationProvider);
        _efConfigurationFactory = _ => Mock.Of<IEFConfiguration>();
        _efOptions = IAMOptions.Create(
            _configuration,
            _securityConfigurationProvider,
            _efConfigurationFactory
        );
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
    public void AddIAMServices_WithMockDb_ReturnsServiceCollection()
    {
        var services = CreateServiceCollection();

        var result = services.AddIAMServices(_mockDbOptions);

        Assert.Same(services, result);
    }

    [Fact]
    public void AddIAMServices_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddIAMServices(_mockDbOptions)
        );
        Assert.Equal("serviceCollection", exception.ParamName);
    }

    [Fact]
    public void AddIAMServices_WithNullOptions_ThrowsArgumentNullException()
    {
        var services = CreateServiceCollection();

        var exception = Assert.Throws<ArgumentNullException>(() => services.AddIAMServices(null!));
        Assert.Equal("options", exception.ParamName);
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
    [InlineData(typeof(IResourceTypeRegistry))]
    public void AddIAMServices_WithMockDb_RegistersService(Type serviceType)
    {
        var services = CreateServiceCollection();

        services.AddIAMServices(_mockDbOptions);
        var serviceProvider = services.BuildServiceProvider();

        var service = serviceProvider.GetService(serviceType);
        Assert.NotNull(service);
    }

    [Fact]
    public void AddIAMServices_WithMockDb_UserContextProvider_ReturnsSameInstanceForBothInterfaces()
    {
        var services = CreateServiceCollection();
        services.AddIAMServices(_mockDbOptions);
        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var userContextProvider = scope.ServiceProvider.GetRequiredService<IUserContextProvider>();
        var userContextSetter = scope.ServiceProvider.GetRequiredService<IUserContextSetter>();

        Assert.Same(userContextProvider, userContextSetter);
    }

    [Theory]
    [InlineData(typeof(ISymmetricEncryptionProviderFactory))]
    [InlineData(typeof(IAsymmetricEncryptionProviderFactory))]
    [InlineData(typeof(IAsymmetricSignatureProviderFactory))]
    [InlineData(typeof(IHashProviderFactory))]
    [InlineData(typeof(ISecurityProvider))]
    [InlineData(typeof(IResourceTypeRegistry))]
    public void AddIAMServices_WithMockDb_SingletonServices_ReturnsSameInstance(Type serviceType)
    {
        var services = CreateServiceCollection();
        services.AddIAMServices(_mockDbOptions);
        var serviceProvider = services.BuildServiceProvider();

        var instance1 = serviceProvider.GetService(serviceType);
        var instance2 = serviceProvider.GetService(serviceType);

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
    public void AddIAMServices_WithMockDb_ScopedServices_ReturnsDifferentInstancePerScope(
        Type serviceType
    )
    {
        var services = CreateServiceCollection();
        services.AddIAMServices(_mockDbOptions);
        var serviceProvider = services.BuildServiceProvider();

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

        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void AddIAMServices_WithMockDb_CanBuildServiceProvider()
    {
        var services = CreateServiceCollection();

        services.AddIAMServices(_mockDbOptions);
        var exception = Record.Exception(() => services.BuildServiceProvider());

        Assert.Null(exception);
    }

    [Fact]
    public void AddIAMServices_WithMockDb_RegistersIResourceTypeRegistry()
    {
        var services = CreateServiceCollection();

        services.AddIAMServices(_mockDbOptions);
        var serviceProvider = services.BuildServiceProvider();

        var registry = serviceProvider.GetService<IResourceTypeRegistry>();
        Assert.NotNull(registry);
    }

    [Theory]
    [InlineData(PermissionConstants.ACCOUNT_RESOURCE_TYPE)]
    [InlineData(PermissionConstants.USER_RESOURCE_TYPE)]
    [InlineData(PermissionConstants.GROUP_RESOURCE_TYPE)]
    [InlineData(PermissionConstants.ROLE_RESOURCE_TYPE)]
    [InlineData(PermissionConstants.PERMISSION_RESOURCE_TYPE)]
    [InlineData(PermissionConstants.ALL_RESOURCE_TYPES)]
    public void AddIAMServices_WithMockDb_ResourceTypeRegistry_ContainsIAMDefinedTypes(
        string resourceType
    )
    {
        var services = CreateServiceCollection();
        services.AddIAMServices(_mockDbOptions);
        var serviceProvider = services.BuildServiceProvider();

        var registry = serviceProvider.GetRequiredService<IResourceTypeRegistry>();

        Assert.True(registry.Exists(resourceType));
    }

    [Fact]
    public void AddIAMServices_WithMockDb_ResourceTypeRegistry_ContainsCustomTypes()
    {
        var services = CreateServiceCollection();
        var options = IAMOptions
            .Create(_configuration, _securityConfigurationProvider)
            .RegisterResourceType("invoice", "Invoices");

        services.AddIAMServices(options);
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IResourceTypeRegistry>();

        Assert.True(registry.Exists("invoice"));
        var info = registry.Get("invoice");
        Assert.NotNull(info);
        Assert.Equal("invoice", info.Name);
        Assert.Equal("Invoices", info.Description);
    }

    [Fact]
    public void AddIAMServices_WithEF_ReturnsServiceCollection()
    {
        var services = CreateServiceCollection();

        var result = services.AddIAMServices(_efOptions);

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
    public void AddIAMServices_WithEF_RegistersService(Type serviceType)
    {
        var services = CreateServiceCollection();

        services.AddIAMServices(_efOptions);
        var serviceProvider = services.BuildServiceProvider();

        var service = serviceProvider.GetService(serviceType);
        Assert.NotNull(service);
    }

    [Fact]
    public void AddIAMServices_WithEF_RegistersIEFConfiguration()
    {
        var services = CreateServiceCollection();

        services.AddIAMServices(_efOptions);
        var serviceProvider = services.BuildServiceProvider();

        var efConfiguration = serviceProvider.GetService<IEFConfiguration>();
        Assert.NotNull(efConfiguration);
    }

    [Fact]
    public void AddIAMServices_WithEF_CanBuildServiceProvider()
    {
        var services = CreateServiceCollection();

        services.AddIAMServices(_efOptions);
        var exception = Record.Exception(() => services.BuildServiceProvider());

        Assert.Null(exception);
    }
}
