using Corely.IAM.DataAccessMigrations.Cli.Commands.PlatformCommands;

namespace Corely.IAM.DataAccessMigrations.Cli.UnitTests;

public class BootstrapSettingsTests : IDisposable
{
    private const string EXACTLY_ONE = "Pass exactly one of --settings";

    private readonly string _directory = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_directory, true);

    private string Bootstrap(params string[] settingsArgs) =>
        CommandRunner.Run(
            new Bootstrap(),
            [
                "--secrets",
                Path.Combine(_directory, "secrets.txt"),
                "--email",
                "owner@example.com",
                .. settingsArgs,
            ]
        );

    [Fact]
    public void Bootstrap_IsRefused_WithNeitherSettingsFlag() =>
        Assert.Contains(EXACTLY_ONE, Bootstrap());

    [Fact]
    public void Bootstrap_IsRefused_WithBothSettingsFlags()
    {
        var settings = Path.Combine(_directory, "appsettings.json");
        File.WriteAllText(settings, "{}");

        Assert.Contains(EXACTLY_ONE, Bootstrap("--settings", settings, "--use-defaults"));
    }

    [Fact]
    public void Bootstrap_IsRefused_WhenTheSettingsFileIsMissing() =>
        Assert.Contains(
            "Settings file not found",
            Bootstrap("--settings", Path.Combine(_directory, "missing.json"))
        );

    [Fact]
    public void Bootstrap_IsRefused_WhenTheSettingsFileIsNotJson()
    {
        var settings = Path.Combine(_directory, "appsettings.json");
        File.WriteAllText(settings, "{ not json");

        Assert.Contains("Cannot read the settings file", Bootstrap("--settings", settings));
    }

    [Fact]
    public void Bootstrap_IsRefused_WhenTheSettingsAreOutOfRange()
    {
        var settings = Path.Combine(_directory, "appsettings.json");
        File.WriteAllText(settings, """{ "SecurityOptions": { "MaxLoginAttempts": 0 } }""");

        var output = Bootstrap("--settings", settings);

        Assert.Contains("The security settings in configuration are invalid", output);
        Assert.Contains("'Max Login Attempts'", output);
        Assert.False(File.Exists(Path.Combine(_directory, "secrets.txt")));
    }

    [Fact]
    public void Bootstrap_WritesNoSecretsFile_WhenTheSettingsAreRefused()
    {
        Bootstrap();

        Assert.False(File.Exists(Path.Combine(_directory, "secrets.txt")));
    }
}
