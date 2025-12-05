using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Services;
using Corely.IAM.Users.Processors;
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

        ServiceCollection.AddSingleton<ISecurityProcessor, SecurityProcessor>();
        ServiceCollection.AddScoped<IPasswordValidationProvider, PasswordValidationProvider>();

        ServiceCollection.AddScoped(serviceProvider => GetSecurityConfigurationProvider());
        ServiceCollection.Configure<SecurityOptions>(
            Configuration.GetSection(SecurityOptions.NAME)
        );
        ServiceCollection.Configure<PasswordValidationOptions>(
            Configuration.GetSection(PasswordValidationOptions.NAME)
        );

        ServiceCollection.AddScoped<IAccountProcessor, AccountProcessor>();
        ServiceCollection.Decorate<IAccountProcessor, LoggingAccountProcessorDecorator>();

        ServiceCollection.AddScoped<IUserProcessor, UserProcessor>();
        ServiceCollection.AddScoped<IBasicAuthProcessor, BasicAuthProcessor>();
        ServiceCollection.AddScoped<IGroupProcessor, GroupProcessor>();
        ServiceCollection.AddScoped<IRoleProcessor, RoleProcessor>();
        ServiceCollection.AddScoped<IPermissionProcessor, PermissionProcessor>();

        ServiceCollection.AddScoped<IRegistrationService, RegistrationService>();
        ServiceCollection.AddScoped<IDeregistrationService, DeregistrationService>();
        ServiceCollection.AddScoped<ISignInService, SignInService>();

        ServiceCollection.AddLogging(AddLogging);
        AddDataServices();
    }

    protected abstract void AddLogging(ILoggingBuilder builder);
    protected abstract void AddDataServices();
    protected abstract ISecurityConfigurationProvider GetSecurityConfigurationProvider();
}
