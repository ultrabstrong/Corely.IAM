namespace Corely.IAM.DataAccessMigrations.MsSql;

internal static class MsSqlDesignTimeConstants
{
    public const string DesignTimeMarker = "designtimeonly";

    public const string DesignTimeConnectionString =
        $"Server={DesignTimeMarker};Database={DesignTimeMarker};Trusted_Connection=True;TrustServerCertificate=True;";
}
