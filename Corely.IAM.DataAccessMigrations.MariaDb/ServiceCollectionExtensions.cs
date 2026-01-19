using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.MariaDb;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMariaDbIamDbContext(
        this IServiceCollection services,
        string connectionString
    )
    {
        var configuration = new EFMariaDbConfiguration(connectionString);

        services.AddDbContext<IamDbContext>(options =>
        {
            configuration.Configure(options);
        });

        services.AddSingleton<IEFConfiguration>(configuration);

        return services;
    }
}
