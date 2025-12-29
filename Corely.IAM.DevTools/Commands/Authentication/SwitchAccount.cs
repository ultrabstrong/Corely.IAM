using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Authentication;

internal partial class Authentication : CommandBase
{
    internal class SwitchAccount : CommandBase
    {
        [Argument("Account public ID (GUID) to switch to", true)]
        private Guid AccountId { get; init; }

        private readonly IAuthenticationService _authenticationService;
        private readonly IUserContextProvider _userContextProvider;

        public SwitchAccount(
            IAuthenticationService authenticationService,
            IUserContextProvider userContextProvider
        )
            : base("switch-account", "Switch to a specific account")
        {
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!ConfigurationProvider.HasAuthToken)
            {
                Error("No auth token found. Sign in first using 'auth signin'.");
                return;
            }

            // Set context from saved auth token before switching
            var authToken = await ReadAuthTokenAsync();
            if (string.IsNullOrEmpty(authToken))
                return;

            // Set the user context from the auth token (simulating middleware)
            var validationResult = await _userContextProvider.SetUserContextAsync(authToken);
            if (
                validationResult
                != Corely.IAM.Users.Models.UserAuthTokenValidationResultCode.Success
            )
            {
                Error($"Failed to set user context: {validationResult}");
                return;
            }

            try
            {
                var request = new SwitchAccountRequest(AccountId);
                var result = await _authenticationService.SwitchAccountAsync(request);

                if (result.ResultCode == SignInResultCode.Success)
                {
                    await WriteAuthTokenToFileAsync(result);
                    Success($"Switched to account {AccountId}. Auth token updated.");
                }
                else
                {
                    Error($"Switch account failed: {result.ResultCode}");
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        Info(result.Message);
                    }
                }
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }

        private static async Task<string?> ReadAuthTokenAsync()
        {
            try
            {
                var fileContent = await File.ReadAllTextAsync(
                    ConfigurationProvider.AuthTokenFilePath
                );
                var jsonDoc = JsonDocument.Parse(fileContent);

                if (!jsonDoc.RootElement.TryGetProperty("AuthToken", out var authTokenElement))
                {
                    Error("Auth token file does not contain 'AuthToken' property.");
                    return null;
                }

                return authTokenElement.GetString();
            }
            catch (Exception ex)
            {
                Error($"Failed to read auth token: {ex.Message}");
                return null;
            }
        }

        private static async Task WriteAuthTokenToFileAsync(SignInResult result)
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
