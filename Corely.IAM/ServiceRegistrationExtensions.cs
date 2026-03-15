using Corely.DataAccess.Extensions;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.DataAccess;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.GoogleAuths.Providers;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Invitations.Processors;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Permissions.Providers;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.TotpAuths.Providers;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Corely.IAM.Validators.FluentValidators;
using Corely.Security.Encryption.Factories;
using Corely.Security.Hashing.Factories;
using Corely.Security.PasswordValidation.Models;
using Corely.Security.PasswordValidation.Providers;
using Corely.Security.Signature.Factories;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddIAMServices(
        this IServiceCollection serviceCollection,
        IAMOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        ArgumentNullException.ThrowIfNull(options);

        if (options.EFConfigurationFactory != null)
        {
            serviceCollection.AddScoped(options.EFConfigurationFactory);
            serviceCollection.AddDbContext<IamDbContext>();
            serviceCollection.RegisterEntityFrameworkReposAndUoW();
        }
        else
        {
            serviceCollection.RegisterMockReposAndUoW();
        }

        serviceCollection.AddSingleton(TimeProvider.System);

        var registry = new ResourceTypeRegistry();
        foreach (var (name, description) in options.CustomResourceTypes)
            registry.Register(name, description);
        serviceCollection.AddSingleton<IResourceTypeRegistry>(registry);

        serviceCollection.AddValidatorsFromAssemblyContaining<FluentValidationProvider>(
            includeInternalTypes: true
        );
        serviceCollection.AddScoped<IFluentValidatorFactory, FluentValidatorFactory>();
        serviceCollection.AddScoped<IValidationProvider, FluentValidationProvider>();

        serviceCollection.AddSingleton<
            ISymmetricEncryptionProviderFactory,
            SymmetricEncryptionProviderFactory
        >(_ => new SymmetricEncryptionProviderFactory(options.SymmetricEncryptionCode));

        serviceCollection.AddSingleton<
            IAsymmetricEncryptionProviderFactory,
            AsymmetricEncryptionProviderFactory
        >(_ => new AsymmetricEncryptionProviderFactory(options.AsymmetricEncryptionCode));

        serviceCollection.AddSingleton<
            IAsymmetricSignatureProviderFactory,
            AsymmetricSignatureProviderFactory
        >(_ => new AsymmetricSignatureProviderFactory(options.AsymmetricSignatureCode));

        serviceCollection.AddSingleton<IHashProviderFactory, HashProviderFactory>(
            _ => new HashProviderFactory(options.HashCode)
        );

        serviceCollection.AddSingleton<ISecurityProvider, SecurityProvider>();
        serviceCollection.AddScoped<IPasswordValidationProvider, PasswordValidationProvider>();

        serviceCollection.AddSingleton(_ => options.SecurityConfigurationProvider);
        serviceCollection.Configure<SecurityOptions>(
            options.Configuration.GetSection(SecurityOptions.NAME)
        );
        serviceCollection.Configure<PasswordValidationOptions>(
            options.Configuration.GetSection(PasswordValidationOptions.NAME)
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

        // Auth boundary: service decorators check only user/account context (HasUserContext /
        // HasAccountContext). CRUDX permission checks happen at the processor level via
        // IsAuthorizedAsync. Service methods that look "unguarded" are protected at the
        // processor level where the actual domain work is performed.
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
        serviceCollection.AddScoped<IModificationService, ModificationService>();
        serviceCollection.Decorate<
            IModificationService,
            ModificationServiceAuthorizationDecorator
        >();
        serviceCollection.Decorate<IModificationService, ModificationServiceTelemetryDecorator>();
        serviceCollection.AddScoped<IAuthenticationService, AuthenticationService>();
        serviceCollection.Decorate<
            IAuthenticationService,
            AuthenticationServiceTelemetryDecorator
        >();

        serviceCollection.AddScoped<IMfaService, MfaService>();
        serviceCollection.Decorate<IMfaService, MfaServiceAuthorizationDecorator>();
        serviceCollection.Decorate<IMfaService, MfaServiceTelemetryDecorator>();

        serviceCollection.AddScoped<IGoogleAuthService, GoogleAuthService>();
        serviceCollection.Decorate<IGoogleAuthService, GoogleAuthServiceAuthorizationDecorator>();
        serviceCollection.Decorate<IGoogleAuthService, GoogleAuthServiceTelemetryDecorator>();

        serviceCollection.AddScoped<IInvitationService, InvitationService>();
        serviceCollection.Decorate<IInvitationService, InvitationServiceAuthorizationDecorator>();
        serviceCollection.Decorate<IInvitationService, InvitationServiceTelemetryDecorator>();

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

        serviceCollection.AddScoped<IInvitationProcessor, InvitationProcessor>();
        serviceCollection.Decorate<
            IInvitationProcessor,
            InvitationProcessorAuthorizationDecorator
        >();
        serviceCollection.Decorate<IInvitationProcessor, InvitationProcessorTelemetryDecorator>();

        serviceCollection.AddSingleton<ITotpProvider, TotpProvider>();
        serviceCollection.AddScoped<ITotpAuthProcessor, TotpAuthProcessor>();
        serviceCollection.Decorate<ITotpAuthProcessor, TotpAuthProcessorAuthorizationDecorator>();
        serviceCollection.Decorate<ITotpAuthProcessor, TotpAuthProcessorTelemetryDecorator>();

        serviceCollection.AddScoped<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
        serviceCollection.AddScoped<IGoogleAuthProcessor, GoogleAuthProcessor>();
        serviceCollection.Decorate<
            IGoogleAuthProcessor,
            GoogleAuthProcessorAuthorizationDecorator
        >();
        serviceCollection.Decorate<IGoogleAuthProcessor, GoogleAuthProcessorTelemetryDecorator>();

        return serviceCollection;
    }
}
