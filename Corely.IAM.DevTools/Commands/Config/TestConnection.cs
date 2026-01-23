using Microsoft.Data.SqlClient;
using MySqlConnector;

namespace Corely.IAM.DevTools.Commands.Config;

internal partial class Config
{
    internal class TestConnection : CommandBase
    {
        public TestConnection()
            : base("test-connection", "Test the database connection") { }

        protected override async Task ExecuteAsync()
        {
            if (!ConfigurationProvider.HasProvider)
            {
                Error("No provider configured.");
                Info("Run 'provider set <provider>' to set a provider.");
                return;
            }

            if (!ConfigurationProvider.HasConnectionString)
            {
                Error("No connection string configured.");
                Info("Run 'config set-connection <connection-string>' to set a connection string.");
                return;
            }

            var provider = ConfigurationProvider.GetProvider();
            var connectionString = ConfigurationProvider.GetConnectionString();

            try
            {
                Info($"Testing {provider} database connection...");

                bool canConnect = provider switch
                {
                    DatabaseProvider.MySql or DatabaseProvider.MariaDb =>
                        await TestMySqlConnectionAsync(connectionString),
                    DatabaseProvider.MsSql => await TestSqlServerConnectionAsync(connectionString),
                    _ => throw new InvalidOperationException($"Unsupported provider: {provider}"),
                };

                if (canConnect)
                {
                    Success("Successfully connected to the database.");
                }
                else
                {
                    Error("Could not connect to the database.");
                    Info("Verify the connection string in your settings file is correct.");
                    Info("Run 'config show' to view current settings.");
                }
            }
            catch (Exception ex)
            {
                Error($"Connection test failed: {ex.Message}");
                Info(
                    "Check that the database server is running and the connection string is correct."
                );
            }
        }

        private static async Task<bool> TestMySqlConnectionAsync(string connectionString)
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            return connection.State == System.Data.ConnectionState.Open;
        }

        private static async Task<bool> TestSqlServerConnectionAsync(string connectionString)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return connection.State == System.Data.ConnectionState.Open;
        }
    }
}
