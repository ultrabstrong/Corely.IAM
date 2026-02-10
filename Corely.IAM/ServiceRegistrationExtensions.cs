using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.Extensions;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.DataAccess;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Corely.IAM.Validators.FluentValidators;
using Corely.Security.Encryption;
using Corely.Security.Encryption.Factories;
using Corely.Security.Hashing;
using Corely.Security.Hashing.Factories;
using Corely.Security.PasswordValidation.Models;
using Corely.Security.PasswordValidation.Providers;
using Corely.Security.Signature;
using Corely.Security.Signature.Factories;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddIAMServicesWithEF(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        ISecurityConfigurationProvider securityConfigurationProvider,
        Func<IServiceProvider, IEFConfiguration> efConfigurationFactory
    )
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(securityConfigurationProvider);
        ArgumentNullException.ThrowIfNull(efConfigurationFactory);

        serviceCollection.AddScoped(efConfigurationFactory);
        serviceCollection.AddDbContext<IamDbContext>();
        serviceCollection.RegisterEntityFrameworkReposAndUoW();
        serviceCollection.AddIAMServices(configuration, securityConfigurationProvider);
        return serviceCollection;
    }

    public static IServiceCollection AddIAMServicesWithMockDb(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        ISecurityConfigurationProvider securityConfigurationProvider
    )
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(securityConfigurationProvider);

        serviceCollection.RegisterMockReposAndUoW();
        serviceCollection.AddIAMServices(configuration, securityConfigurationProvider);
        return serviceCollection;
    }

    private static void AddIAMServices(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        ISecurityConfigurationProvider securityConfigurationProvider
    )
    {
        serviceCollection.AddSingleton(TimeProvider.System);

        serviceCollection.AddValidatorsFromAssemblyContaining<FluentValidationProvider>(
            includeInternalTypes: true
        );
        serviceCollection.AddScoped<IFluentValidatorFactory, FluentValidatorFactory>();
        serviceCollection.AddScoped<IValidationProvider, FluentValidationProvider>();

        serviceCollection.AddSingleton<
            ISymmetricEncryptionProviderFactory,
            SymmetricEncryptionProviderFactory
        >(serviceProvider => new SymmetricEncryptionProviderFactory(
            SymmetricEncryptionConstants.AES_CODE
        ));

        serviceCollection.AddSingleton<
            IAsymmetricEncryptionProviderFactory,
            AsymmetricEncryptionProviderFactory
        >(serviceProvider => new AsymmetricEncryptionProviderFactory(
            AsymmetricEncryptionConstants.RSA_CODE
        ));

        serviceCollection.AddSingleton<
            IAsymmetricSignatureProviderFactory,
            AsymmetricSignatureProviderFactory
        >(serviceProvider => new AsymmetricSignatureProviderFactory(
            AsymmetricSignatureConstants.ECDSA_SHA256_CODE
        ));

        serviceCollection.AddSingleton<IHashProviderFactory, HashProviderFactory>(
            _ => new HashProviderFactory(HashConstants.SALTED_SHA256_CODE)
        );

        serviceCollection.AddSingleton<ISecurityProvider, SecurityProvider>();
        serviceCollection.AddScoped<IPasswordValidationProvider, PasswordValidationProvider>();

        serviceCollection.AddSingleton(_ => securityConfigurationProvider);
        serviceCollection.Configure<SecurityOptions>(
            configuration.GetSection(SecurityOptions.NAME)
        );
        serviceCollection.Configure<PasswordValidationOptions>(
            configuration.GetSection(PasswordValidationOptions.NAME)
        );

        serviceCollection.AddScoped<IAuthenticationProvider, AuthenticationProvider>();
        serviceCollection.AddScoped<UserContextProvider>();
        serviceCollection.AddScoped<IUserContextProvider>(sp =>
            sp.GetRequiredService<UserContextProvider>()
        );
        serviceCollection.AddScoped<IUserContextSetter>(sp =>
            sp.GetRequiredService<UserContextProvider>()
        );
        serviceCollection.AddScoped<AuthorizationProvider>();
        serviceCollection.AddScoped<IAuthorizationProvider>(sp =>
            sp.GetRequiredService<AuthorizationProvider>()
        );
        serviceCollection.AddScoped<IAuthorizationCacheClearer>(sp =>
            sp.GetRequiredService<AuthorizationProvider>()
        );

        serviceCollection.AddScoped<IRegistrationService, RegistrationService>();
        serviceCollection.Decorate<
            IRegistrationService,
            RegistrationServiceAuthorizationDecorator
        >();
        serviceCollection.Decorate<IRegistrationService, RegistrationServiceTelemetryDecorator>();
        serviceCollection.AddScoped<IDeregistrationService, DeregistrationService>();
        serviceCollection.Decorate<
            IDeregistrationService,
            DeregistrationServiceAuthorizationDecorator
        >();
        serviceCollection.Decorate<
            IDeregistrationService,
            DeregistrationServiceTelemetryDecorator
        >();
        serviceCollection.AddScoped<IRetrievalService, RetrievalService>();
        serviceCollection.Decorate<IRetrievalService, RetrievalServiceAuthorizationDecorator>();
        serviceCollection.Decorate<IRetrievalService, RetrievalServiceTelemetryDecorator>();
        serviceCollection.AddScoped<IAuthenticationService, AuthenticationService>();
        serviceCollection.Decorate<
            IAuthenticationService,
            AuthenticationServiceTelemetryDecorator
        >();

        serviceCollection.AddScoped<IUserOwnershipProcessor, UserOwnershipProcessor>();

        serviceCollection.AddScoped<IAccountProcessor, AccountProcessor>();
        serviceCollection.Decorate<IAccountProcessor, AccountProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<IAccountProcessor, AccountProcessorTelemetryDecorator>();

        serviceCollection.AddScoped<IUserProcessor, UserProcessor>();
        serviceCollection.Decorate<IUserProcessor, UserProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<IUserProcessor, UserProcessorTelemetryDecorator>();

        serviceCollection.AddScoped<IBasicAuthProcessor, BasicAuthProcessor>();
        serviceCollection.Decorate<IBasicAuthProcessor, BasicAuthProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<IBasicAuthProcessor, BasicAuthProcessorTelemetryDecorator>();

        serviceCollection.AddScoped<IGroupProcessor, GroupProcessor>();
        serviceCollection.Decorate<IGroupProcessor, GroupProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<IGroupProcessor, GroupProcessorTelemetryDecorator>();

        serviceCollection.AddScoped<IRoleProcessor, RoleProcessor>();
        serviceCollection.Decorate<IRoleProcessor, RoleProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<IRoleProcessor, RoleProcessorTelemetryDecorator>();

        serviceCollection.AddScoped<IPermissionProcessor, PermissionProcessor>();
        serviceCollection.Decorate<
            IPermissionProcessor,
            PermissionProcessorAuthorizationDecorator
        >();
        serviceCollection.Decorate<IPermissionProcessor, PermissionProcessorTelemetryDecorator>();
    }
}
