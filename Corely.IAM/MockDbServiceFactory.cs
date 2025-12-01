using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.DataAccess.Mock;
using Corely.DataAccess.Mock.Repos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM;

public abstract class MockDbServiceFactory(
    IServiceCollection serviceCollection,
    IConfiguration configuration
) : ServiceFactoryBase(serviceCollection, configuration)
{
    protected sealed override void AddDataServices()
    {
        ServiceCollection.AddSingleton(typeof(IReadonlyRepo<>), typeof(MockReadonlyRepo<>));
        ServiceCollection.AddSingleton(typeof(IRepo<>), typeof(MockRepo<>));
        ServiceCollection.AddSingleton<IUnitOfWorkProvider, MockUoWProvider>();
    }
}
