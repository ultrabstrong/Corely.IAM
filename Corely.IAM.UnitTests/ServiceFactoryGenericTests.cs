using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.IAM.Accounts.Entities;
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
using Corely.Security.Encryption.Factories;
using Corely.Security.Hashing.Factories;
using Corely.Security.PasswordValidation.Models;
using Corely.Security.PasswordValidation.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests;

public abstract class ServiceFactoryGenericTests
{
    protected abstract ServiceFactoryBase CreateServiceFactory(
        IServiceCollection serviceCollection,
        IConfiguration configuration
    );

    [Theory, MemberData(nameof(GetRequiredServiceData))]
    public void ServiceFactoryBase_ProvidesService(Type serviceType)
    {
        var serviceCollection = new ServiceCollection();
        var configuration = new ConfigurationManager();
        var serviceFactory = CreateServiceFactory(serviceCollection, configuration);

        serviceFactory.AddIAMServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var service = serviceProvider.GetRequiredService(serviceType);

        Assert.NotNull(service);
    }

    public static IEnumerable<object[]> GetRequiredServiceData =>
        [
            [typeof(ISecurityConfigurationProvider)],
            [typeof(IPasswordValidationProvider)],
            [typeof(IOptions<PasswordValidationOptions>)],
            [typeof(IOptions<SecurityOptions>)],
            [typeof(IValidationProvider)],
            [typeof(ISymmetricEncryptionProviderFactory)],
            [typeof(IAsymmetricEncryptionProviderFactory)],
            [typeof(IHashProviderFactory)],
            [typeof(IAccountProcessor)],
            [typeof(IUserProcessor)],
            [typeof(IBasicAuthProcessor)],
            [typeof(IGroupProcessor)],
            [typeof(IRoleProcessor)],
            [typeof(IPermissionProcessor)],
            [typeof(IRegistrationService)],
            [typeof(IDeregistrationService)],
            [typeof(ISecurityProcessor)],
            // Repos are registered as generics. Only need to test each one once.
            [typeof(IReadonlyRepo<AccountEntity>)],
            [typeof(IRepo<AccountEntity>)],
            [typeof(IUnitOfWorkProvider)],
        ];
}
