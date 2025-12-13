using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Processors;
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
using Microsoft.Extensions.Logging;

namespace Corely.IAM;

public abstract class ServiceFactoryBase(
    IServiceCollection serviceCollection,
    IConfiguration configuration
)
{
    protected IServiceCollection ServiceCollection { get; } = serviceCollection;
    protected IConfiguration Configuration { get; } = configuration;

    public void AddIAMServices()
    {
        ServiceCollection.AddValidatorsFromAssemblyContaining<FluentValidationProvider>(
            includeInternalTypes: true
        );
        ServiceCollection.AddScoped<IFluentValidatorFactory, FluentValidatorFactory>();
        ServiceCollection.AddScoped<IValidationProvider, FluentValidationProvider>();

        ServiceCollection.AddSingleton<
            ISymmetricEncryptionProviderFactory,
            SymmetricEncryptionProviderFactory
        >(serviceProvider => new SymmetricEncryptionProviderFactory(
            SymmetricEncryptionConstants.AES_CODE
        ));

        ServiceCollection.AddSingleton<
            IAsymmetricEncryptionProviderFactory,
            AsymmetricEncryptionProviderFactory
        >(serviceProvider => new AsymmetricEncryptionProviderFactory(
            AsymmetricEncryptionConstants.RSA_CODE
        ));

        ServiceCollection.AddSingleton<
            IAsymmetricSignatureProviderFactory,
            AsymmetricSignatureProviderFactory
        >(serviceProvider => new AsymmetricSignatureProviderFactory(
            AsymmetricSignatureConstants.ECDSA_SHA256_CODE
        ));

        ServiceCollection.AddSingleton<IHashProviderFactory, HashProviderFactory>(
            _ => new HashProviderFactory(HashConstants.SALTED_SHA256_CODE)
        );

        ServiceCollection.AddSingleton<ISecurityProvider, SecurityProvider>();
        ServiceCollection.AddScoped<IPasswordValidationProvider, PasswordValidationProvider>();

        ServiceCollection.AddScoped(serviceProvider => GetSecurityConfigurationProvider());
        ServiceCollection.Configure<SecurityOptions>(
            Configuration.GetSection(SecurityOptions.NAME)
        );
        ServiceCollection.Configure<PasswordValidationOptions>(
            Configuration.GetSection(PasswordValidationOptions.NAME)
        );

        ServiceCollection.AddScoped<IAuthenticationProvider, AuthenticationProvider>();
        ServiceCollection.AddScoped<IamUserContextProvider>();
        ServiceCollection.AddScoped<IIamUserContextProvider>(sp =>
            sp.GetRequiredService<IamUserContextProvider>()
        );
        ServiceCollection.AddScoped<IIamUserContextSetter>(sp =>
            sp.GetRequiredService<IamUserContextProvider>()
        );
        ServiceCollection.AddScoped<IAuthorizationProvider, AuthorizationProvider>();

        ServiceCollection.AddScoped<IRegistrationService, RegistrationService>();
        ServiceCollection.Decorate<
            IRegistrationService,
            RegistrationServiceAuthorizationDecorator
        >();
        ServiceCollection.AddScoped<IDeregistrationService, DeregistrationService>();
        ServiceCollection.AddScoped<IAuthenticationService, AuthenticationService>();
        ServiceCollection.AddScoped<IRetrievalService, RetrievalService>();

        ServiceCollection.AddLogging(AddLogging);
        AddDataServices();

        ServiceCollection.AddScoped<IUserOwnershipProcessor, UserOwnershipProcessor>();

        ServiceCollection.AddScoped<IAccountProcessor, AccountProcessor>();
        ServiceCollection.Decorate<IAccountProcessor, AccountProcessorAuthorizationDecorator>();
        ServiceCollection.Decorate<IAccountProcessor, AccountProcessorLoggingDecorator>();

        ServiceCollection.AddScoped<IUserProcessor, UserProcessor>();
        ServiceCollection.Decorate<IUserProcessor, UserProcessorAuthorizationDecorator>();
        ServiceCollection.Decorate<IUserProcessor, UserProcessorLoggingDecorator>();

        ServiceCollection.AddScoped<IBasicAuthProcessor, BasicAuthProcessor>();
        ServiceCollection.Decorate<IBasicAuthProcessor, BasicAuthProcessorAuthorizationDecorator>();
        ServiceCollection.Decorate<IBasicAuthProcessor, BasicAuthProcessorLoggingDecorator>();

        ServiceCollection.AddScoped<IGroupProcessor, GroupProcessor>();
        ServiceCollection.Decorate<IGroupProcessor, GroupProcessorAuthorizationDecorator>();
        ServiceCollection.Decorate<IGroupProcessor, GroupProcessorLoggingDecorator>();

        ServiceCollection.AddScoped<IRoleProcessor, RoleProcessor>();
        ServiceCollection.Decorate<IRoleProcessor, RoleProcessorAuthorizationDecorator>();
        ServiceCollection.Decorate<IRoleProcessor, RoleProcessorLoggingDecorator>();

        ServiceCollection.AddScoped<IPermissionProcessor, PermissionProcessor>();
        ServiceCollection.Decorate<
            IPermissionProcessor,
            PermissionProcessorAuthorizationDecorator
        >();
        ServiceCollection.Decorate<IPermissionProcessor, PermissionProcessorLoggingDecorator>();
    }

    protected abstract void AddLogging(ILoggingBuilder builder);
    protected abstract void AddDataServices();
    protected abstract ISecurityConfigurationProvider GetSecurityConfigurationProvider();
}
