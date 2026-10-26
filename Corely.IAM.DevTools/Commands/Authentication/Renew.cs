using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Authentication;

internal partial class Authentication : CommandBase
{
    internal class Renew : CommandBase
    {
        private readonly IAuthenticationService _authenticationService;

        public Renew(IAuthenticationService authenticationService)
            : base("renew", "Renew the saved auth token with its refresh token")
        {
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (!ConfigurationProvider.HasAuthToken)
            {
                Error("No auth token found. Sign in first using 'auth signin'.");
                return;
            }

            var refreshToken = await ReadRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            var result = await _authenticationService.RenewAuthTokenAsync(new(refreshToken));
            if (result.ResultCode != RenewAuthTokenResultCode.Success)
            {
                Error($"Auth token renewal failed: {result.ResultCode}");
                if (!string.IsNullOrWhiteSpace(result.Message))
                    Info(result.Message);

                return;
            }

            await WriteAuthTokenToFileAsync(result);
            Success("Auth token renewed successfully.");
        }

        private static async Task<string?> ReadRefreshTokenAsync()
        {
            try
            {
                var fileContent = await File.ReadAllTextAsync(
                    ConfigurationProvider.AuthTokenFilePath
                );
                var jsonDoc = JsonDocument.Parse(fileContent);

                if (
                    !jsonDoc.RootElement.TryGetProperty("RefreshToken", out var refreshTokenElement)
                )
                {
                    Error("Auth token file does not contain 'RefreshToken' property.");
                    return null;
                }

                return refreshTokenElement.GetString();
            }
            catch (Exception ex)
            {
                Error($"Failed to read auth token: {ex.Message}");
                return null;
            }
        }

        private static async Task WriteAuthTokenToFileAsync(RenewAuthTokenResult result)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var resultJson = JsonSerializer.Serialize(result, options);
                await File.WriteAllTextAsync(ConfigurationProvider.AuthTokenFilePath, resultJson);
                Info($"Auth token saved to: {ConfigurationProvider.AuthTokenFilePath}");
            }
            catch (Exception ex)
            {
                Error($"Failed to save auth token: {ex.Message}");
            }
        }
    }
}
