using Corely.DataAccess.EntityFramework.Configurations;
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
        var provider = ConfigurationProvider.TryGetProvider();

        if (
            !string.IsNullOrEmpty(encryptionKey)
            && !string.IsNullOrEmpty(connectionString)
            && provider.HasValue
        )
        {
            var securityConfigurationProvider = new SecurityConfigurationProvider(encryptionKey);

            Func<IServiceProvider, IEFConfiguration> efConfig = provider.Value switch
            {
                DatabaseProvider.MySql => sp => new MySqlEFConfiguration(
                    connectionString,
                    sp.GetRequiredService<ILoggerFactory>()
                ),
                DatabaseProvider.MariaDb => sp => new MySqlEFConfiguration(
                    connectionString,
                    sp.GetRequiredService<ILoggerFactory>()
                ),
                DatabaseProvider.MsSql => sp => new MsSqlEFConfiguration(
                    connectionString,
                    sp.GetRequiredService<ILoggerFactory>()
                ),
                _ => throw new InvalidOperationException($"Unsupported provider: {provider}"),
            };

            services.AddIAMServicesWithEF(configuration, securityConfigurationProvider, efConfig);
        }

        return services;
    }
}
