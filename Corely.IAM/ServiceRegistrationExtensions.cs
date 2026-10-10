using Corely.DataAccess.Extensions;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.DataAccess;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.GoogleAuths.Providers;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Invitations.Processors;
using Corely.IAM.PasswordRecoveries.Constants;
using Corely.IAM.PasswordRecoveries.Processors;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Permissions.Providers;
using Corely.IAM.Platform;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Processors;
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
using Corely.Security.Secrets;
using Corely.Security.Signature.Factories;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM;

public static class ServiceRegistrationExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection AddIAMServices(IAMOptions options)
        {
            ArgumentNullException.ThrowIfNull(serviceCollection);
            ArgumentNullException.ThrowIfNull(options);

            if (options.EFConfigurationFactory != null)
            {
                var efConfigurationFactory = options.EFConfigurationFactory;
                serviceCollection.AddKeyedScoped(
                    EFConfigurationKeys.IAM,
                    (sp, _) => efConfigurationFactory(sp)
                );
                serviceCollection.AddDbContext<IamDbContext>();
                serviceCollection.RegisterEntityFrameworkReposAndUoW();
            }
            else
            {
                serviceCollection.RegisterMockReposAndUoW();
            }

            serviceCollection.AddSingleton(TimeProvider.System);

            var registry = new ResourceTypeRegistry();
            foreach (var type in options.CustomResourceTypes.Values)
                registry.Register(type.Name, type.Description, type.OwnerActions);
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
            serviceCollection.AddSingleton(
                new IamHashCodes(options.HashCode, options.TokenHashCode)
            );
            serviceCollection.AddSingleton<ISecretProvider>(_ => new RandomSecretProvider(
                PasswordRecoveryConstants.TOKEN_SECRET_LENGTH
            ));

            serviceCollection.AddSingleton<ISecurityProvider, SecurityProvider>();
            serviceCollection.AddScoped<IPasswordValidationProvider, PasswordValidationProvider>();

            serviceCollection.AddSingleton(_ => options.SecurityConfigurationProvider);
            serviceCollection.Configure<SecurityOptions>(
                options.Configuration.GetSection(SecurityOptions.NAME)
            );
            serviceCollection.Configure<PasswordValidationOptions>(
                options.Configuration.GetSection(PasswordValidationOptions.NAME)
            );

            serviceCollection.Configure<AuditOptions>(
                options.Configuration.GetSection(AuditOptions.NAME)
            );
            serviceCollection.AddSingleton<AuditSettingsCache>();
            serviceCollection.AddScoped<IAuditPolicy, AuditPolicy>();
            serviceCollection.AddScoped<IAuditProvider, AuditProvider>();
            serviceCollection.AddScoped<IAuditAccessProvider, AuditAccessProvider>();

            serviceCollection.AddScoped<IPlatformAccessProvider, PlatformAccessProvider>();
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
            serviceCollection.Decorate<IRegistrationService, RegistrationServiceAuditDecorator>();
            serviceCollection.Decorate<
                IRegistrationService,
                RegistrationServiceTelemetryDecorator
            >();
            serviceCollection.AddScoped<IDeregistrationService, DeregistrationService>();
            serviceCollection.Decorate<
                IDeregistrationService,
                DeregistrationServiceAuthorizationDecorator
            >();
            serviceCollection.Decorate<
                IDeregistrationService,
                DeregistrationServiceAuditDecorator
            >();
            serviceCollection.Decorate<
                IDeregistrationService,
                DeregistrationServiceTelemetryDecorator
            >();
            serviceCollection.AddScoped<IRetrievalService, RetrievalService>();
            serviceCollection.Decorate<IRetrievalService, RetrievalServiceAuditDecorator>();
            serviceCollection.Decorate<IRetrievalService, RetrievalServiceTelemetryDecorator>();
            serviceCollection.AddScoped<IPlatformService, PlatformService>();
            serviceCollection.Decorate<IPlatformService, PlatformServiceAuditDecorator>();
            serviceCollection.AddHostedService<PlatformOwnerPermissionsStartup>();
            serviceCollection.AddScoped<IModificationService, ModificationService>();
            serviceCollection.Decorate<IModificationService, ModificationServiceAuditDecorator>();
            serviceCollection.Decorate<
                IModificationService,
                ModificationServiceTelemetryDecorator
            >();
            serviceCollection.AddScoped<IAuthenticationService, AuthenticationService>();
            serviceCollection.Decorate<
                IAuthenticationService,
                AuthenticationServiceAuditDecorator
            >();
            serviceCollection.Decorate<
                IAuthenticationService,
                AuthenticationServiceTelemetryDecorator
            >();

            serviceCollection.AddScoped<IMfaService, MfaService>();
            serviceCollection.Decorate<IMfaService, MfaServiceAuthorizationDecorator>();
            serviceCollection.Decorate<IMfaService, MfaServiceAuditDecorator>();
            serviceCollection.Decorate<IMfaService, MfaServiceTelemetryDecorator>();

            serviceCollection.AddScoped<IGoogleAuthService, GoogleAuthService>();
            serviceCollection.Decorate<
                IGoogleAuthService,
                GoogleAuthServiceAuthorizationDecorator
            >();
            serviceCollection.Decorate<IGoogleAuthService, GoogleAuthServiceAuditDecorator>();
            serviceCollection.Decorate<IGoogleAuthService, GoogleAuthServiceTelemetryDecorator>();

            serviceCollection.AddScoped<IInvitationService, InvitationService>();
            serviceCollection.Decorate<IInvitationService, InvitationServiceAuditDecorator>();
            serviceCollection.Decorate<IInvitationService, InvitationServiceTelemetryDecorator>();

            serviceCollection.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
            serviceCollection.Decorate<
                IPasswordRecoveryService,
                PasswordRecoveryServiceAuditDecorator
            >();
            serviceCollection.Decorate<
                IPasswordRecoveryService,
                PasswordRecoveryServiceTelemetryDecorator
            >();

            serviceCollection.AddScoped<IAuditService, AuditService>();
            serviceCollection.Decorate<IAuditService, AuditServiceAuthorizationDecorator>();
            serviceCollection.Decorate<IAuditService, AuditServiceAuditDecorator>();
            serviceCollection.Decorate<IAuditService, AuditServiceTelemetryDecorator>();

            serviceCollection.AddScoped<IUserOwnershipProcessor, UserOwnershipProcessor>();

            serviceCollection.AddScoped<IAccountProcessor, AccountProcessor>();
            serviceCollection.Decorate<IAccountProcessor, AccountProcessorAuthorizationDecorator>();
            serviceCollection.Decorate<IAccountProcessor, AccountProcessorTelemetryDecorator>();

            serviceCollection.AddScoped<IKeyRotationProcessor, KeyRotationProcessor>();
            serviceCollection.Decorate<
                IKeyRotationProcessor,
                KeyRotationProcessorAuthorizationDecorator
            >();
            serviceCollection.Decorate<
                IKeyRotationProcessor,
                KeyRotationProcessorTelemetryDecorator
            >();

            serviceCollection.AddScoped<IUserProcessor, UserProcessor>();
            serviceCollection.Decorate<IUserProcessor, UserProcessorAuthorizationDecorator>();
            serviceCollection.Decorate<IUserProcessor, UserProcessorTelemetryDecorator>();

            serviceCollection.AddScoped<BasicAuthProcessor>();
            serviceCollection.AddScoped<IBasicAuthProcessor>(sp =>
                sp.GetRequiredService<BasicAuthProcessor>()
            );
            serviceCollection.Decorate<
                IBasicAuthProcessor,
                BasicAuthProcessorAuthorizationDecorator
            >();
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
            serviceCollection.Decorate<
                IPermissionProcessor,
                PermissionProcessorTelemetryDecorator
            >();

            serviceCollection.AddScoped<IInvitationProcessor, InvitationProcessor>();
            serviceCollection.Decorate<
                IInvitationProcessor,
                InvitationProcessorAuthorizationDecorator
            >();
            serviceCollection.Decorate<
                IInvitationProcessor,
                InvitationProcessorTelemetryDecorator
            >();

            serviceCollection.AddScoped<IPasswordRecoveryProcessor, PasswordRecoveryProcessor>();
            serviceCollection.Decorate<
                IPasswordRecoveryProcessor,
                PasswordRecoveryProcessorTelemetryDecorator
            >();

            serviceCollection.AddSingleton<ITotpProvider, TotpProvider>();
            serviceCollection.AddScoped<ITotpAuthProcessor, TotpAuthProcessor>();
            serviceCollection.Decorate<
                ITotpAuthProcessor,
                TotpAuthProcessorAuthorizationDecorator
            >();
            serviceCollection.Decorate<ITotpAuthProcessor, TotpAuthProcessorTelemetryDecorator>();

            serviceCollection.AddScoped<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
            serviceCollection.AddScoped<IGoogleAuthProcessor, GoogleAuthProcessor>();
            serviceCollection.Decorate<
                IGoogleAuthProcessor,
                GoogleAuthProcessorAuthorizationDecorator
            >();
            serviceCollection.Decorate<
                IGoogleAuthProcessor,
                GoogleAuthProcessorTelemetryDecorator
            >();

            return serviceCollection;
        }
    }
}
