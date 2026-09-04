using Corely.IAM.DataAccess;
using Corely.IAM.DataAccessMigrations.Cli.Attributes;
using Corely.IAM.DataAccessMigrations.MsSql;
using Corely.IAM.DataAccessMigrations.MySql;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands;

internal abstract class DbCommandBase(string name, string description)
    : CommandBase(name, description)
{
    [Option(
        "-p",
        "--provider",
        Description = $"Database provider (MySql, MsSql). Falls back to {ConnectionSettings.PROVIDER_VARIABLE}."
    )]
    private string ProviderName { get; init; } = null!;

    [Option(
        "-c",
        "--connection-string",
        Description = $"Database connection string. Falls back to {ConnectionSettings.CONNECTION_VARIABLE}."
    )]
    private string ConnectionString { get; init; } = null!;

    [Option(
        "--history-table",
        Description = "Migrations history table. Defaults to "
            + MigrationConstants.DEFAULT_HISTORY_TABLE
            + ". Pass __EFMigrationsHistory for a database migrated before that default existed."
    )]
    private string HistoryTable { get; init; } = null!;

    /// <summary>
    /// Script generation resolves entirely from the migrations assembly, so it needs a provider
    /// but never opens a connection.
    /// </summary>
    protected virtual bool RequiresConnectionString => true;

    protected bool TryCreateDbContext(out IamDbContext dbContext)
    {
        dbContext = null!;

        var providerResolution = ConnectionSettings.TryResolveProvider(
            ProviderName,
            out var provider
        );
        if (!Report(providerResolution))
            return false;

        var connectionResolution = ConnectionSettings.TryResolveConnectionString(
            ConnectionString,
            out var connectionString
        );
        if (!connectionResolution.IsValid)
        {
            if (RequiresConnectionString)
            {
                Report(connectionResolution);
                return false;
            }
            connectionString = PlaceholderConnectionString(provider);
        }

        var historyTable = string.IsNullOrWhiteSpace(HistoryTable) ? null : HistoryTable;

        dbContext = provider switch
        {
            DatabaseProvider.MsSql => MsSqlIamDbContextFactory.Create(
                connectionString,
                historyTable
            ),
            DatabaseProvider.MySql => MySqlIamDbContextFactory.Create(
                connectionString,
                historyTable
            ),
            _ => throw new NotSupportedException($"Unsupported provider: {provider}"),
        };

        return true;
    }

    protected async Task<bool> TryConnectAsync(IamDbContext dbContext)
    {
        try
        {
            if (await dbContext.Database.CanConnectAsync())
                return true;

            Error("Could not connect to the database.");
            Info("Verify the connection string is correct and the database server is reachable.");
        }
        catch (Exception ex)
        {
            Error($"Database connection failed: {ex.Message}");
            Info("Check that the database server is running and the connection string is correct.");
        }
        return false;
    }

    private static bool Report(ConnectionSettings.Resolution resolution)
    {
        if (resolution.IsValid)
            return true;

        Error(resolution.ErrorMessage!);
        if (!string.IsNullOrEmpty(resolution.Guidance))
            Info(resolution.Guidance);
        return false;
    }

    // EF needs a syntactically valid connection string to build options even when nothing will
    // open it.
    private static string PlaceholderConnectionString(DatabaseProvider provider) =>
        provider switch
        {
            DatabaseProvider.MsSql => "Server=.;Database=CorelyIam;Trusted_Connection=True;",
            _ => "Server=localhost;Database=CorelyIam;Uid=root;Pwd=;",
        };
}
