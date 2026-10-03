using Corely.IAM.DataAccessMigrations.Cli.Attributes;
using Corely.IAM.DataAccessMigrations.Cli.Extensions;
using Corely.IAM.DataAccessMigrations.Cli.Iam;
using Corely.IAM.Platform.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands.PlatformCommands;

internal class Bootstrap()
    : DbCommandBase(
        "bootstrap",
        "Create the platform account, its owner user and the owner's credentials, once"
    )
{
    private const string SYSTEM_KEY_VARIABLE = "CORELY_IAM_SYSTEM_KEY";

    [Option(
        "-s",
        "--secrets",
        Description = "File to write the owner's password, authenticator secret and recovery codes to. Must not exist yet."
    )]
    private string SecretsPath { get; init; } = null!;

    [Option("-e", "--email", Description = "The platform owner's email address.")]
    private string Email { get; init; } = null!;

    [Option(
        "-u",
        "--username",
        Description = "The platform owner's username. Defaults to platform-owner."
    )]
    private string Username { get; init; } = null!;

    [Option(
        "-a",
        "--account-name",
        Description = "The platform account's name. Defaults to Platform."
    )]
    private string AccountName { get; init; } = null!;

    [Option(
        "-k",
        "--system-key",
        Description = $"The app's system encryption key (hex). Falls back to {SYSTEM_KEY_VARIABLE}."
    )]
    private string SystemKey { get; init; } = null!;

    protected override async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(SecretsPath))
        {
            Error("Pass --secrets with a file to write the owner's credentials to.");
            return;
        }
        if (string.IsNullOrWhiteSpace(Email))
        {
            Error("Pass --email with the platform owner's email address.");
            return;
        }
        var systemKey = string.IsNullOrWhiteSpace(SystemKey)
            ? Environment.GetEnvironmentVariable(SYSTEM_KEY_VARIABLE)
            : SystemKey;
        if (string.IsNullOrWhiteSpace(systemKey))
        {
            Error($"Pass --system-key, or set {SYSTEM_KEY_VARIABLE}: the app's system key.");
            return;
        }
        if (!TryResolveConnection(out var provider, out var connectionString))
            return;

        var secretsPath = Path.GetFullPath(SecretsPath);
        FileStream secretsFile;
        try
        {
            secretsFile = new FileStream(secretsPath, FileMode.CreateNew, FileAccess.Write);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Error($"Cannot create the secrets file {secretsPath}: {ex.Message}");
            Info("Name a file that does not exist yet, in a folder that does.");
            return;
        }

        var written = false;
        try
        {
            await using var services = BuildIamServices(provider, connectionString, systemKey);
            await using var scope = services.CreateAsyncScope();
            var result = await scope
                .ServiceProvider.GetRequiredService<IPlatformService>()
                .BootstrapPlatformAsync(
                    new BootstrapPlatformRequest(
                        string.IsNullOrWhiteSpace(AccountName) ? "Platform" : AccountName,
                        string.IsNullOrWhiteSpace(Username) ? "platform-owner" : Username,
                        Email
                    )
                );

            if (result.ResultCode != BootstrapPlatformResultCode.Success)
            {
                Error($"Bootstrap failed ({result.ResultCode}): {result.Message}");
                return;
            }

            await using (var writer = new StreamWriter(secretsFile))
            {
                await writer.WriteAsync(
                    result.Credentials!.ToSecretsFileText(TimeProvider.System.GetUtcNow())
                );
            }
            written = true;

            Success(
                $"Created the platform account {result.Credentials!.AccountName} owned by {result.Credentials.Username}."
            );
            Info(
                $"The owner's credentials are in {secretsPath}. Store them safely, then delete it."
            );
        }
        finally
        {
            if (!written)
            {
                await secretsFile.DisposeAsync();
                File.Delete(secretsPath);
            }
        }
    }

    private static ServiceProvider BuildIamServices(
        DatabaseProvider provider,
        string connectionString,
        string systemKey
    )
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIAMServices(
            IAMOptions.Create(
                new ConfigurationBuilder().Build(),
                new SystemKeyConfigurationProvider(systemKey),
                _ =>
                    provider switch
                    {
                        DatabaseProvider.MsSql => new MsSqlEFConfiguration(connectionString),
                        DatabaseProvider.MySql => new MySqlEFConfiguration(connectionString),
                        _ => throw new NotSupportedException($"Unsupported provider: {provider}"),
                    }
            )
        );
        return services.BuildServiceProvider();
    }
}
