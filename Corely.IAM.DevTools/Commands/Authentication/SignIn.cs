using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Authentication;

internal partial class Authentication : CommandBase
{
    internal class SignIn : CommandBase
    {
        [Argument("Filepath to sign in request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IAuthenticationService _authenticationService;

        public SignIn(IAuthenticationService authenticationService)
            : base("signin", "Sign in a user and get an auth token")
        {
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (Create)
            {
                SampleJsonFileHelper.CreateSampleSingleRequestJson(
                    RequestJsonFile,
                    new SignInRequest("userName", "password", "devtools-device-id")
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await SignInAsync();
            }
        }

        private async Task SignInAsync()
        {
            var request = SampleJsonFileHelper.ReadSingleRequestJson<SignInRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            try
            {
                var result = await _authenticationService.SignInAsync(request);

                if (result.ResultCode == SignInResultCode.Success)
                {
                    await WriteAuthTokenToFileAsync(result);
                    Success("Sign in successful. Auth token saved.");
                }
                else
                {
                    Warn($"Sign in failed: {result.ResultCode}");
                }
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
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
