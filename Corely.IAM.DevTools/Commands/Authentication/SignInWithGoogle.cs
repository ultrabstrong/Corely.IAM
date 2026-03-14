using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Authentication;

internal partial class Authentication : CommandBase
{
    internal class SignInWithGoogle : CommandBase
    {
        [Argument("Filepath to a file containing the Google ID token", true)]
        private string IdTokenFile { get; init; } = null!;

        private readonly IAuthenticationService _authenticationService;

        public SignInWithGoogle(IAuthenticationService authenticationService)
            : base("signin-google", "Sign in with a Google ID token")
        {
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (!FileExists(IdTokenFile))
                return;

            var idToken = (await File.ReadAllTextAsync(IdTokenFile)).Trim();
            var request = new SignInWithGoogleRequest(idToken, "devtools-device-id");
            var result = await _authenticationService.SignInWithGoogleAsync(request);

            if (result.ResultCode == SignInResultCode.Success)
            {
                await WriteAuthTokenToFileAsync(result);
                Success("Google sign in successful. Auth token saved.");
            }
            else if (result.ResultCode == SignInResultCode.MfaRequiredChallenge)
            {
                Warn("MFA required.");
                Info($"Challenge token: {result.MfaChallengeToken}");
                Info("Run 'auth verify-mfa <challenge-token> <code>' to complete sign in.");
            }
            else
            {
                Error($"Google sign in failed: {result.ResultCode} - {result.Message}");
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
