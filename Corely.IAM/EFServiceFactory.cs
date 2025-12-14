using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.Extensions;
using Corely.IAM.DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM;

public abstract class EFServiceFactory(
    IServiceCollection serviceCollection,
    IConfiguration configuration
) : ServiceFactoryBase(serviceCollection, configuration)
{
    protected sealed override void AddDataServices()
    {
        ServiceCollection.AddScoped(GetEFConfiguration);
        ServiceCollection.AddDbContext<IamDbContext>();
        ServiceCollection.RegisterEntityFrameworkReposAndUoW();
    }

    protected abstract IEFConfiguration GetEFConfiguration(IServiceProvider sp);
}
