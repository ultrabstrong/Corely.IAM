using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Authentication;

internal partial class Authentication : CommandBase
{
    internal class VerifyMfa : CommandBase
    {
        [Argument("MFA challenge token from sign in", true)]
        private string ChallengeToken { get; init; } = null!;

        [Argument("TOTP code or recovery code", true)]
        private string Code { get; init; } = null!;

        private readonly IAuthenticationService _authenticationService;

        public VerifyMfa(IAuthenticationService authenticationService)
            : base("verify-mfa", "Complete MFA verification after sign in")
        {
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            var request = new VerifyMfaRequest(ChallengeToken, Code);
            var result = await _authenticationService.VerifyMfaAsync(request);

            if (result.ResultCode == SignInResultCode.Success)
            {
                await WriteAuthTokenToFileAsync(result);
                Success("MFA verification successful. Auth token saved.");
            }
            else
            {
                Error($"MFA verification failed: {result.ResultCode} - {result.Message}");
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
