namespace Corely.IAM.DataAccessMigrations.MySql;

internal static class MySqlDesignTimeConstants
{
    public const string DesignTimeMarker = "designtimeonly";

    public const string DesignTimeConnectionString =
        $"Server={DesignTimeMarker};Port=1;Database={DesignTimeMarker};Uid={DesignTimeMarker};Pwd={DesignTimeMarker};";
}
