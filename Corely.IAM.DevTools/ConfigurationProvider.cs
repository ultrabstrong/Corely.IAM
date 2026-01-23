using Microsoft.Extensions.Configuration;

namespace Corely.IAM.DevTools;

internal static class ConfigurationProvider
{
    public const string SETTINGS_FILE_NAME = "corely-iam-devtool-settings.json";
    public const string AUTH_TOKEN_FILE_NAME = "corely-iam-auth-token.json";

    private static readonly IConfigurationRoot _configuration;

    static ConfigurationProvider()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(SETTINGS_FILE_NAME, optional: true, reloadOnChange: true);

        _configuration = builder.Build();
    }

    public static IConfiguration Configuration => _configuration;

    public static DatabaseProvider? TryGetProvider()
    {
        var value = _configuration["Provider"];
        if (DatabaseProviderExtensions.TryParse(value, out var provider))
        {
            return provider;
        }
        return null;
    }

    public static DatabaseProvider GetProvider()
    {
        return TryGetProvider()
            ?? throw new InvalidOperationException(
                $"Provider not found or invalid in {SETTINGS_FILE_NAME}. "
                    + $"Run 'provider set <provider>' to set a provider. "
                    + $"Valid providers: {string.Join(", ", DatabaseProviderExtensions.GetNames())}"
            );
    }

    public static string? TryGetConnectionString()
    {
        return _configuration.GetConnectionString("DataRepoConnection");
    }

    public static string GetConnectionString()
    {
        return TryGetConnectionString()
            ?? throw new InvalidOperationException(
                $"DataRepoConnection string not found in {SETTINGS_FILE_NAME}. "
                    + "Run 'config init' to create a settings file or 'config path' to see the expected location."
            );
    }

    public static string? TryGetSystemSymmetricEncryptionKey()
    {
        return _configuration["SystemSymmetricEncryptionKey"];
    }

    public static string GetSystemSymmetricEncryptionKey()
    {
        return TryGetSystemSymmetricEncryptionKey()
            ?? throw new InvalidOperationException(
                $"SystemSymmetricEncryptionKey not found in {SETTINGS_FILE_NAME}. "
                    + "Run 'config show' to view current settings."
            );
    }

    public static bool HasConnectionString => TryGetConnectionString() != null;

    public static bool HasSystemSymmetricEncryptionKey =>
        !string.IsNullOrEmpty(TryGetSystemSymmetricEncryptionKey());

    public static bool HasProvider => TryGetProvider() != null;

    public static string SettingsFilePath =>
        Path.Combine(AppContext.BaseDirectory, SETTINGS_FILE_NAME);

    public static string AuthTokenFilePath =>
        Path.Combine(AppContext.BaseDirectory, AUTH_TOKEN_FILE_NAME);

    public static bool HasAuthToken => File.Exists(AuthTokenFilePath);
}
