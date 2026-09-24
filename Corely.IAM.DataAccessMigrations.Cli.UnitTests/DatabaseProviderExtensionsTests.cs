namespace Corely.IAM.DataAccessMigrations.Cli.UnitTests;

public class DatabaseProviderExtensionsTests
{
    [Fact]
    public void PlaceholderConnectionString_UsesWindowsAuth_ForMsSql()
    {
        Assert.Equal(
            "Server=.;Database=CorelyIam;Trusted_Connection=True;",
            DatabaseProvider.MsSql.PlaceholderConnectionString()
        );
    }

    [Fact]
    public void PlaceholderConnectionString_UsesRootOnLocalhost_ForMySql()
    {
        Assert.Equal(
            "Server=localhost;Database=CorelyIam;Uid=root;Pwd=;",
            DatabaseProvider.MySql.PlaceholderConnectionString()
        );
    }
}
