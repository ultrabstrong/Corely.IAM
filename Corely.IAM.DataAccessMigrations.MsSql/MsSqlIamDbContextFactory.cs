using Corely.IAM.DataAccess;

namespace Corely.IAM.DataAccessMigrations.MsSql;

internal static class MsSqlIamDbContextFactory
{
    public static IamDbContext Create(string connectionString, string? historyTable = null) =>
        new(new EFMsSqlConfiguration(connectionString, historyTable));
}
