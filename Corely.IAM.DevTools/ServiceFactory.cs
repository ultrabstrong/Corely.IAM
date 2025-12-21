using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Corely.IAM.DevTools;

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

        // Only register IAM services if configuration is available
        // This allows config commands to work without a settings file
        var encryptionKey = configuration["SystemSymmetricEncryptionKey"];
        var connectionString = configuration.GetConnectionString("DataRepoConnection");

        if (!string.IsNullOrEmpty(encryptionKey) && !string.IsNullOrEmpty(connectionString))
        {
            var securityConfigurationProvider = new SecurityConfigurationProvider(encryptionKey);

            services.AddIAMServicesWithEF(
                configuration,
                securityConfigurationProvider,
                sp => new MySqlEFConfiguration(
                    connectionString,
                    sp.GetRequiredService<ILoggerFactory>()
                )
            );
        }

        return services;
    }
}
