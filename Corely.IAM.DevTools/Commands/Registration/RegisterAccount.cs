using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Registration;

internal partial class Registration : CommandBase
{
    internal class RegisterAccount : CommandBase
    {
        [Argument("Filepath to register account request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IRegistrationService _registrationService;
        private readonly IAuthenticationService _authenticationService;

        public RegisterAccount(
            IRegistrationService registrationService,
            IAuthenticationService authenticationService
        )
            : base("account", "Register a new account")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (Create)
            {
                SampleJsonFileHelper.CreateSampleMultipleRequestJson(
                    RequestJsonFile,
                    new RegisterAccountRequest("accountName", Guid.Empty)
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await RegisterAccountAsync();
            }
        }

        private async Task RegisterAccountAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var request = SampleJsonFileHelper.ReadMultipleRequestJson<RegisterAccountRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            foreach (var registerRequest in request)
            {
                var result = await _registrationService.RegisterAccountAsync(registerRequest);
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
