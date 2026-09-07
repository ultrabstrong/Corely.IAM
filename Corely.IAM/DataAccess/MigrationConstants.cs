namespace Corely.IAM.DataAccess;

internal static class MigrationConstants
{
    /// <summary>
    /// Kept separate from the default __EFMigrationsHistory so IAM can share a database with a
    /// consumer's own contexts.
    /// </summary>
    public const string DEFAULT_HISTORY_TABLE = "__CorelyIamMigrationsHistory";
}
