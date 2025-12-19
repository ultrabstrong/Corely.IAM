using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Corely.IAM.ConsoleApp;

internal static class ServiceFactory
{
    public static IServiceCollection RegisterServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(logger: Log.Logger, dispose: false);
        });

        services.AddScoped<IEFConfiguration>(sp => new MySqlEFConfiguration(
            configuration.GetConnectionString("DataRepoConnection")
                ?? throw new Exception($"DataRepoConnection string not found in configuration"),
            sp.GetRequiredService<ILoggerFactory>()
        ));

        var securityConfigurationProvider = new SecurityConfigurationProvider(
            configuration["SystemSymmetricEncryptionKey"]
                ?? throw new Exception($"SystemSymmetricEncryptionKey not found in configuration")
        );

        services.AddIAMServicesWithEF(configuration, securityConfigurationProvider);

        return services;
    }
}
