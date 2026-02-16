using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.WebApp.DataAccess;

public class MsSqlEFConfiguration(string connectionString, ILoggerFactory loggerFactory)
    : EFMsSqlConfigurationBase(connectionString)
{
    private readonly ILogger _efLogger = loggerFactory.CreateLogger("EFCore");

    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlServer(connectionString)
            .LogTo(
                logger: e =>
                    EFEventDataLogger.Write(_efLogger, e, EFEventDataLogger.WriteInfoLogsAs.Trace),
                filter: (eventId, _) => eventId.Id == RelationalEventId.CommandExecuted.Id
            );
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging().EnableDetailedErrors();
#endif
    }
}
