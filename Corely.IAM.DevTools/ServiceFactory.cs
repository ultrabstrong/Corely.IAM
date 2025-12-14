using System.Reflection;
using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Corely.IAM.DevTools;

internal class ServiceFactory(IServiceCollection servicesCollection, IConfiguration configuration)
    : EFServiceFactory(servicesCollection, configuration)
{
    protected override void AddLogging(ILoggingBuilder builder) =>
        builder.AddSerilog(logger: Log.Logger, dispose: false);

    protected override ISecurityConfigurationProvider GetSecurityConfigurationProvider() =>
        new SecurityConfigurationProvider(
            Configuration["SystemSymmetricEncryptionKey"]
                ?? throw new Exception($"SystemSymmetricEncryptionKey not found in configuration")
        );

    protected override IEFConfiguration GetEFConfiguration(IServiceProvider sp) =>
        new MySqlEFConfiguration(
            Configuration.GetConnectionString("DataRepoConnection")
                ?? throw new Exception($"DataRepoConnection string not found in configuration"),
            sp.GetRequiredService<ILoggerFactory>()
        );

    private class MySqlEFConfiguration(string connectionString, ILoggerFactory loggerFactory)
        : EFMySqlConfigurationBase(connectionString)
    {
        private readonly Microsoft.Extensions.Logging.ILogger _efLogger =
            loggerFactory.CreateLogger("EFCore");

        public override void Configure(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    b => b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name)
                )
                .LogTo(
                    logger: e =>
                        EFEventDataLogger.Write(
                            _efLogger,
                            e,
                            EFEventDataLogger.WriteInfoLogsAs.Trace
                        ),
                    filter: (eventId, _) => eventId.Id == RelationalEventId.CommandExecuted.Id
                );
        }
    }
}
