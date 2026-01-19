using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Corely.IAM.DataAccessMigrations.MySql;

internal static class MySqlDesignTimeConstants
{
    public const string DesignTimeMarker = "designtimeonly";

    public const string DesignTimeConnectionString =
        $"Server={DesignTimeMarker};Port=1;Database={DesignTimeMarker};Uid={DesignTimeMarker};Pwd={DesignTimeMarker};";

    public static readonly ServerVersion DesignTimeServerVersion = ServerVersion.Create(
        Version.Parse("8.0.0"),
        ServerType.MySql
    );
}
