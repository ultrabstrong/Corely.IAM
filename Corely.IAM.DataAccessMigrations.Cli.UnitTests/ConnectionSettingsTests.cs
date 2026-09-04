namespace Corely.IAM.DataAccessMigrations.Cli.UnitTests;

public class ConnectionSettingsTests
{
    private static Func<string, string?> Environment(
        string? provider = null,
        string? connection = null
    ) =>
        name =>
            name switch
            {
                ConnectionSettings.PROVIDER_VARIABLE => provider,
                ConnectionSettings.CONNECTION_VARIABLE => connection,
                _ => null,
            };

    [Theory]
    [InlineData("MsSql", DatabaseProvider.MsSql)]
    [InlineData("mssql", DatabaseProvider.MsSql)]
    [InlineData("MySql", DatabaseProvider.MySql)]
    public void TryResolveProvider_ParsesOption_CaseInsensitively(
        string value,
        DatabaseProvider expected
    )
    {
        var result = ConnectionSettings.TryResolveProvider(value, out var provider, Environment());

        Assert.True(result.IsValid);
        Assert.Equal(expected, provider);
    }

    [Fact]
    public void TryResolveProvider_FallsBackToEnvironment_WhenOptionMissing()
    {
        var result = ConnectionSettings.TryResolveProvider(
            null,
            out var provider,
            Environment(provider: "MySql")
        );

        Assert.True(result.IsValid);
        Assert.Equal(DatabaseProvider.MySql, provider);
    }

    [Fact]
    public void TryResolveProvider_PrefersOption_OverEnvironment()
    {
        var result = ConnectionSettings.TryResolveProvider(
            "MsSql",
            out var provider,
            Environment(provider: "MySql")
        );

        Assert.True(result.IsValid);
        Assert.Equal(DatabaseProvider.MsSql, provider);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolveProvider_TreatsBlankOption_AsAbsent(string? option)
    {
        var result = ConnectionSettings.TryResolveProvider(
            option,
            out var provider,
            Environment(provider: "MsSql")
        );

        Assert.True(result.IsValid);
        Assert.Equal(DatabaseProvider.MsSql, provider);
    }

    [Fact]
    public void TryResolveProvider_Fails_WhenNeitherSourceHasValue()
    {
        var result = ConnectionSettings.TryResolveProvider(null, out _, Environment());

        Assert.False(result.IsValid);
        Assert.Contains(ConnectionSettings.PROVIDER_VARIABLE, result.Guidance);
    }

    [Fact]
    public void TryResolveProvider_Fails_WhenValueIsNotAProvider()
    {
        var result = ConnectionSettings.TryResolveProvider("Postgres", out _, Environment());

        Assert.False(result.IsValid);
        Assert.Contains("Postgres", result.ErrorMessage);
    }

    [Fact]
    public void TryResolveConnectionString_FallsBackToEnvironment_WhenOptionMissing()
    {
        var result = ConnectionSettings.TryResolveConnectionString(
            null,
            out var connectionString,
            Environment(connection: "Server=env;")
        );

        Assert.True(result.IsValid);
        Assert.Equal("Server=env;", connectionString);
    }

    [Fact]
    public void TryResolveConnectionString_PrefersOption_OverEnvironment()
    {
        var result = ConnectionSettings.TryResolveConnectionString(
            "Server=option;",
            out var connectionString,
            Environment(connection: "Server=env;")
        );

        Assert.True(result.IsValid);
        Assert.Equal("Server=option;", connectionString);
    }

    [Fact]
    public void TryResolveConnectionString_Fails_WhenNeitherSourceHasValue()
    {
        var result = ConnectionSettings.TryResolveConnectionString(null, out _, Environment());

        Assert.False(result.IsValid);
        Assert.Contains(ConnectionSettings.CONNECTION_VARIABLE, result.Guidance);
    }
}
