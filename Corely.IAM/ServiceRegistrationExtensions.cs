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
        ISecurityConfigurationProvider securityConfigurationProvider
    )
    {
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

        serviceCollection.AddScoped(_ => securityConfigurationProvider);
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
        serviceCollection.AddScoped<IAuthorizationProvider, AuthorizationProvider>();

        serviceCollection.AddScoped<IRegistrationService, RegistrationService>();
        serviceCollection.Decorate<
            IRegistrationService,
            RegistrationServiceAuthorizationDecorator
        >();
        serviceCollection.Decorate<IRegistrationService, RegistrationServiceLoggingDecorator>();
        serviceCollection.AddScoped<IDeregistrationService, DeregistrationService>();
        serviceCollection.Decorate<
            IDeregistrationService,
            DeregistrationServiceAuthorizationDecorator
        >();
        serviceCollection.Decorate<IDeregistrationService, DeregistrationServiceLoggingDecorator>();
        serviceCollection.AddScoped<IAuthenticationService, AuthenticationService>();
        serviceCollection.Decorate<IAuthenticationService, AuthenticationServiceLoggingDecorator>();

        serviceCollection.AddScoped<IUserOwnershipProcessor, UserOwnershipProcessor>();

        serviceCollection.AddScoped<IAccountProcessor, AccountProcessor>();
        serviceCollection.Decorate<IAccountProcessor, AccountProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<IAccountProcessor, AccountProcessorLoggingDecorator>();

        serviceCollection.AddScoped<IUserProcessor, UserProcessor>();
        serviceCollection.Decorate<IUserProcessor, UserProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<IUserProcessor, UserProcessorLoggingDecorator>();

        serviceCollection.AddScoped<IBasicAuthProcessor, BasicAuthProcessor>();
        serviceCollection.Decorate<IBasicAuthProcessor, BasicAuthProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<IBasicAuthProcessor, BasicAuthProcessorLoggingDecorator>();

        serviceCollection.AddScoped<IGroupProcessor, GroupProcessor>();
        serviceCollection.Decorate<IGroupProcessor, GroupProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<IGroupProcessor, GroupProcessorLoggingDecorator>();

        serviceCollection.AddScoped<IRoleProcessor, RoleProcessor>();
        serviceCollection.Decorate<IRoleProcessor, RoleProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<IRoleProcessor, RoleProcessorLoggingDecorator>();

        serviceCollection.AddScoped<IPermissionProcessor, PermissionProcessor>();
        serviceCollection.Decorate<
            IPermissionProcessor,
            PermissionProcessorAuthorizationDecorator
        >();
        serviceCollection.Decorate<IPermissionProcessor, PermissionProcessorLoggingDecorator>();
    }
}
