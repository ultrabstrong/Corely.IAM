namespace Corely.IAM.IntegrationTests.Providers;

/// <summary>
/// The three providers the library actually ships. Each has its own migration assembly, and until
/// this tier existed none of them had ever been executed against their real database by a test.
/// </summary>
public enum DatabaseProvider
{
    MsSql,
    MySql,
    MariaDb,
}
