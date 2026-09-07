namespace Corely.IAM.DataAccessMigrations.Cli;

/// <summary>
/// Resolves the provider and connection string from command-line options, falling back to
/// environment variables.
/// </summary>
/// <remarks>
/// No settings file: an installed tool's would sit in its install directory, shared machine-wide.
/// </remarks>
internal static class ConnectionSettings
{
    public const string PROVIDER_VARIABLE = "CORELY_IAM_DB_PROVIDER";
    public const string CONNECTION_VARIABLE = "CORELY_IAM_DB_CONNECTION";

    public record Resolution(bool IsValid, string? ErrorMessage = null, string? Guidance = null);

    public static Resolution TryResolveProvider(
        string? option,
        out DatabaseProvider provider,
        Func<string, string?>? readEnvironment = null
    )
    {
        provider = default;
        readEnvironment ??= Environment.GetEnvironmentVariable;

        var value = FirstNonBlank(option, readEnvironment(PROVIDER_VARIABLE));
        var providers = string.Join(", ", DatabaseProviderExtensions.GetNames());

        if (value == null)
        {
            return new Resolution(
                false,
                "No database provider specified.",
                $"Pass --provider, or set {PROVIDER_VARIABLE}. Valid providers: {providers}"
            );
        }

        if (!DatabaseProviderExtensions.TryParse(value, out provider))
        {
            return new Resolution(
                false,
                $"Invalid database provider: {value}",
                $"Valid providers: {providers}"
            );
        }

        return new Resolution(true);
    }

    public static Resolution TryResolveConnectionString(
        string? option,
        out string connectionString,
        Func<string, string?>? readEnvironment = null
    )
    {
        readEnvironment ??= Environment.GetEnvironmentVariable;

        var value = FirstNonBlank(option, readEnvironment(CONNECTION_VARIABLE));
        connectionString = value ?? string.Empty;

        if (value == null)
        {
            return new Resolution(
                false,
                "No connection string specified.",
                $"Pass --connection-string, or set {CONNECTION_VARIABLE}."
            );
        }

        return new Resolution(true);
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
