using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.ConsoleApp;

public sealed class InMemoryConfig(string dbName, ILoggerFactory loggerFactory)
    : EFInMemoryConfigurationBase
{
    private readonly ILogger _efLogger = loggerFactory.CreateLogger("EFCore");

    public override void Configure(DbContextOptionsBuilder b) =>
        b.UseInMemoryDatabase(dbName)
            .LogTo(
                logger: e =>
                    EFEventDataLogger.Write(_efLogger, e, EFEventDataLogger.WriteInfoLogsAs.Trace),
                filter: (eventId, _) => eventId.Id == RelationalEventId.CommandExecuted.Id
            )
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
}
