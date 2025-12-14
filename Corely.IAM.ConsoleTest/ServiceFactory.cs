using System.Reflection;
using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Corely.IAM.ConsoleApp;

internal class ServiceFactory(IServiceCollection serviceCollection, IConfiguration configuration)
    : EFServiceFactory(serviceCollection, configuration)
{
    private readonly IConfiguration _configuration = configuration;

    protected override void AddLogging(ILoggingBuilder builder) =>
        builder.AddSerilog(logger: Log.Logger, dispose: false);

    protected override ISecurityConfigurationProvider GetSecurityConfigurationProvider() =>
        new SecurityConfigurationProvider(
            _configuration["SystemSymmetricEncryptionKey"]
                ?? throw new Exception($"SystemSymmetricEncryptionKey not found in configuration")
        );

    protected override IEFConfiguration GetEFConfiguration(IServiceProvider sp) =>
        new MySqlEFConfiguration(
            _configuration.GetConnectionString("DataRepoConnection")
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
#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging().EnableDetailedErrors();
#endif
        }
    }

    public sealed class InMemoryConfig(string dbName, ILoggerFactory loggerFactory)
        : EFInMemoryConfigurationBase
    {
        private readonly Microsoft.Extensions.Logging.ILogger _efLogger =
            loggerFactory.CreateLogger("EFCore");

        public override void Configure(DbContextOptionsBuilder b) =>
            b.UseInMemoryDatabase(dbName)
                .LogTo(
                    logger: e =>
                        EFEventDataLogger.Write(
                            _efLogger,
                            e,
                            EFEventDataLogger.WriteInfoLogsAs.Trace
                        ),
                    filter: (eventId, _) => eventId.Id == RelationalEventId.CommandExecuted.Id
                )
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
    }
}
