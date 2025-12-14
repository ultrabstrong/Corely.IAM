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

        [Option("-o", "--output", Description = "Optional filepath to output the auth token")]
        private string? OutputTokenFile { get; init; }

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
                SampleJsonFileHelper.CreateSampleJson(
                    RequestJsonFile,
                    new SignInRequest("userName", "password", Guid.Empty)
                );
            }
            else
            {
                await SignInAsync();
            }
        }

        private async Task SignInAsync()
        {
            var requests = SampleJsonFileHelper.ReadRequestJson<SignInRequest>(RequestJsonFile);
            if (requests == null)
                return;

            try
            {
                foreach (var request in requests)
                {
                    var result = await _authenticationService.SignInAsync(request);
                    Console.WriteLine(JsonSerializer.Serialize(result));

                    if (
                        !string.IsNullOrEmpty(OutputTokenFile)
                        && result.ResultCode == SignInResultCode.Success
                        && !string.IsNullOrEmpty(result.AuthToken)
                    )
                    {
                        await WriteTokenToFileAsync(result.AuthToken);
                    }
                }
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }

        private async Task WriteTokenToFileAsync(string authToken)
        {
            try
            {
                var file = new FileInfo(OutputTokenFile!);
                if (!Directory.Exists(file.DirectoryName))
                {
                    Warn($"Directory not found: {file.DirectoryName}");
                    return;
                }

                await File.WriteAllTextAsync(OutputTokenFile!, authToken);
                Success($"Auth token written to: {OutputTokenFile}");
            }
            catch (Exception ex)
            {
                Error($"Failed to write auth token to file: {ex.Message}");
            }
        }
    }
}
