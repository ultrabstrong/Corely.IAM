using Corely.IAM.DataAccessMigrations.Cli.Commands.DatabaseCommands;

namespace Corely.IAM.DataAccessMigrations.Cli.UnitTests;

/// <summary>
/// Drives the commands through the real parse-and-invoke pipeline, which is the only place the
/// options declared on DbCommandBase are actually bound.
/// </summary>
public class DbCommandTests : IDisposable
{
    private readonly string? _originalProvider;
    private readonly string? _originalConnection;

    public DbCommandTests()
    {
        _originalProvider = Environment.GetEnvironmentVariable(
            ConnectionSettings.PROVIDER_VARIABLE
        );
        _originalConnection = Environment.GetEnvironmentVariable(
            ConnectionSettings.CONNECTION_VARIABLE
        );
        SetEnvironment(null, null);
    }

    public void Dispose() => SetEnvironment(_originalProvider, _originalConnection);

    private static void SetEnvironment(string? provider, string? connection)
    {
        Environment.SetEnvironmentVariable(ConnectionSettings.PROVIDER_VARIABLE, provider);
        Environment.SetEnvironmentVariable(ConnectionSettings.CONNECTION_VARIABLE, connection);
    }

    [Fact]
    public void Migrate_ReportsMissingProvider_WhenNothingIsConfigured()
    {
        var output = CommandRunner.Run(new Migrate());

        Assert.Contains("No database provider specified", output);
    }

    [Fact]
    public void Migrate_ReportsMissingConnectionString_WhenOnlyProviderIsGiven()
    {
        var output = CommandRunner.Run(new Migrate(), "--provider", "MsSql");

        Assert.Contains("No connection string specified", output);
    }

    [Fact]
    public void Migrate_ReadsProviderFromEnvironment()
    {
        SetEnvironment("MsSql", null);

        var output = CommandRunner.Run(new Migrate());

        Assert.DoesNotContain("No database provider specified", output);
        Assert.Contains("No connection string specified", output);
    }

    [Fact]
    public void Script_GeneratesWithoutAConnectionString()
    {
        var output = CommandRunner.Run(new Script(), "--provider", "MsSql");

        Assert.DoesNotContain("No connection string specified", output);
        Assert.Contains("CREATE TABLE", output);
    }

    [Fact]
    public void Script_UsesIamHistoryTable_ByDefault()
    {
        var output = CommandRunner.Run(new Script(), "--provider", "MsSql");

        Assert.Contains("__CorelyIamMigrationsHistory", output);
    }

    [Fact]
    public void Script_HonorsHistoryTableOverride()
    {
        var output = CommandRunner.Run(
            new Script(),
            "--provider",
            "MsSql",
            "--history-table",
            "__EFMigrationsHistory"
        );

        Assert.DoesNotContain("__CorelyIamMigrationsHistory", output);
        Assert.Contains("__EFMigrationsHistory", output);
    }

    [Fact]
    public void ProviderOption_TakesPrecedenceOverEnvironment()
    {
        SetEnvironment("MySql", null);

        var output = CommandRunner.Run(new Script(), "--provider", "MsSql");

        // Bracket-quoted identifiers are SQL Server syntax; MySQL would render backticks.
        Assert.Contains("[__CorelyIamMigrationsHistory]", output);
    }
}
