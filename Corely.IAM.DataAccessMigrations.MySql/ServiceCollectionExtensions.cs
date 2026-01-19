using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.MySql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMySqlIamDbContext(
        this IServiceCollection services,
        string connectionString
    )
    {
        var configuration = new EFMySqlConfiguration(connectionString);

        services.AddDbContext<IamDbContext>(options =>
        {
            configuration.Configure(options);
        });

        services.AddSingleton<IEFConfiguration>(configuration);

        return services;
    }
}
