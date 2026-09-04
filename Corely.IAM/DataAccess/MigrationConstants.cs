namespace Corely.IAM.DataAccess;

internal static class MigrationConstants
{
    /// <summary>
    /// IAM records its migrations in its own history table so it can share a database with a
    /// consumer's own contexts without every context writing to the default
    /// __EFMigrationsHistory. Databases migrated before this default existed can keep the old
    /// table by overriding it.
    /// </summary>
    public const string DEFAULT_HISTORY_TABLE = "__CorelyIamMigrationsHistory";
}
