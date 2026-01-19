using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.MsSql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMsSqlIamDbContext(
        this IServiceCollection services,
        string connectionString
    )
    {
        var configuration = new EFMsSqlConfiguration(connectionString);

        services.AddDbContext<IamDbContext>(options =>
        {
            configuration.Configure(options);
        });

        services.AddSingleton<IEFConfiguration>(configuration);

        return services;
    }
}
