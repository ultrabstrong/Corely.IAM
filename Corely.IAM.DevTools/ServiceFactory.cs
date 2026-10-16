using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.TotpAuths.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Corely.IAM.DevTools;

internal static class ServiceFactory
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterServices(IConfiguration configuration)
        {
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(logger: Log.Logger, dispose: false);
            });

            var missing = ConfigurationProvider.MissingIamSettings;
            if (missing.Count > 0)
            {
                AddWithoutIam(
                    services,
                    $"{ConfigurationProvider.SETTINGS_FILE_NAME} is missing {string.Join(", ", missing)}. Run 'config show' to see it."
                );
                return services;
            }

            var encryptionKey = configuration["SystemSymmetricEncryptionKey"]!;
            var connectionString = configuration.GetConnectionString("DataRepoConnection")!;
            var provider = ConfigurationProvider.GetProvider();

            var securityConfigurationProvider = new SecurityConfigurationProvider(encryptionKey);

            Func<IServiceProvider, IEFConfiguration> efConfig = provider switch
            {
                DatabaseProvider.MySql => sp => new MySqlEFConfiguration(
                    connectionString,
                    sp.GetRequiredService<ILoggerFactory>()
                ),
                DatabaseProvider.MsSql => sp => new MsSqlEFConfiguration(
                    connectionString,
                    sp.GetRequiredService<ILoggerFactory>()
                ),
                _ => throw new InvalidOperationException($"Unsupported provider: {provider}"),
            };

            var options = IAMOptions.Create(configuration, securityConfigurationProvider, efConfig);
            try
            {
                services.AddIAMServices(options);
            }
            catch (InvalidOperationException ex)
            {
                AddWithoutIam(services, ex.Message);
            }

            return services;
        }
    }

    private static void AddWithoutIam(IServiceCollection services, string reason)
    {
        services.AddSingleton(new IamServicesUnavailable(reason));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITotpProvider, TotpProvider>();
    }
}
