using Corely.IAM.DataAccess;

namespace Corely.IAM.DataAccessMigrations.MySql;

internal static class MySqlIamDbContextFactory
{
    public static IamDbContext Create(string connectionString, string? historyTable = null) =>
        new(new EFMySqlConfiguration(connectionString, historyTable));
}
