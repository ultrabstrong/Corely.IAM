using Corely.IAM.DataAccessMigrations.Cli.Commands.DatabaseCommands;
using Corely.IAM.DataAccessMigrations.Cli.Commands.PlatformCommands;

namespace Corely.IAM.DataAccessMigrations.Cli.UnitTests;

public class ExitCodeTests : IDisposable
{
    private readonly string? _originalProvider = Environment.GetEnvironmentVariable(
        ConnectionSettings.PROVIDER_VARIABLE
    );
    private readonly string? _originalConnection = Environment.GetEnvironmentVariable(
        ConnectionSettings.CONNECTION_VARIABLE
    );

    public ExitCodeTests()
    {
        Environment.SetEnvironmentVariable(ConnectionSettings.PROVIDER_VARIABLE, null);
        Environment.SetEnvironmentVariable(ConnectionSettings.CONNECTION_VARIABLE, null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(ConnectionSettings.PROVIDER_VARIABLE, _originalProvider);
        Environment.SetEnvironmentVariable(
            ConnectionSettings.CONNECTION_VARIABLE,
            _originalConnection
        );
    }

    [Fact]
    public void Migrate_ExitsWithOne_WhenItReportsAnError() =>
        Assert.Equal(1, CommandRunner.ExitCode(new Migrate()));

    [Fact]
    public void Bootstrap_ExitsWithOne_WhenItReportsAnError() =>
        Assert.Equal(1, CommandRunner.ExitCode(new Bootstrap()));

    [Fact]
    public void Script_ExitsWithZero_WhenItSucceeds() =>
        Assert.Equal(0, CommandRunner.ExitCode(new Script(), "--provider", "MsSql"));
}
