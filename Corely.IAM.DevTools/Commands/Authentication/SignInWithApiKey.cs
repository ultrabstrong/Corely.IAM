using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Authentication;

internal partial class Authentication : CommandBase
{
    internal class SignInWithApiKey : CommandBase
    {
        [Argument("The API key, ciam_<prefix>_<secret>", true)]
        private string ApiKey { get; init; } = null!;

        [Option("-d", "--device", Description = "Device id for the session")]
        private string? DeviceId { get; init; }

        private readonly IAuthenticationService _authenticationService;

        public SignInWithApiKey(IAuthenticationService authenticationService)
            : base("apikey", "Exchange an API key for an auth token")
        {
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            var result = await _authenticationService.SignInWithApiKeyAsync(
                new SignInWithApiKeyRequest(ApiKey, DeviceId ?? "devtools-api-key")
            );
            if (result.ResultCode != SignInResultCode.Success)
            {
                Warn($"API key sign in failed: {result.ResultCode}");
                return;
            }

            try
            {
                var resultJson = JsonSerializer.Serialize(
                    result,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                await File.WriteAllTextAsync(ConfigurationProvider.AuthTokenFilePath, resultJson);
                Success(
                    $"Signed in with the API key. Tokens saved to: {ConfigurationProvider.AuthTokenFilePath}"
                );
            }
            catch (Exception ex)
            {
                Error($"Failed to save auth token: {ex.Message}");
            }
        }
    }
}
