using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Corely.IAM.DataAccessMigrations.MariaDb;

internal static class MariaDbDesignTimeConstants
{
    public const string DesignTimeMarker = "designtimeonly";

    public const string DesignTimeConnectionString =
        $"Server={DesignTimeMarker};Port=1;Database={DesignTimeMarker};Uid={DesignTimeMarker};Pwd={DesignTimeMarker};";

    public static readonly ServerVersion DesignTimeServerVersion = ServerVersion.Create(
        Version.Parse("10.6.0"),
        ServerType.MariaDb
    );
}
