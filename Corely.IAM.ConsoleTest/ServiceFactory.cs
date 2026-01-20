using Corely.DataAccess.EntityFramework.Configurations;
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

        var securityConfigurationProvider = new SecurityConfigurationProvider(
            configuration["SystemSymmetricEncryptionKey"]
                ?? throw new Exception($"SystemSymmetricEncryptionKey not found in configuration")
        );

        Func<IServiceProvider, IEFConfiguration> efConfig;

        var connectionString =
            configuration.GetConnectionString("DataRepoConnection")
            ?? throw new Exception($"DataRepoConnection string not found in configuration");

        bool useMySql = false;
        if (useMySql)
            efConfig = sp => new MySqlEFConfiguration(
                connectionString,
                sp.GetRequiredService<ILoggerFactory>()
            );
        else
            efConfig = sp => new MsSqlEFConfiguration(
                connectionString,
                sp.GetRequiredService<ILoggerFactory>()
            );

        services.AddIAMServicesWithEF(configuration, securityConfigurationProvider, efConfig);

        return services;
    }
}
