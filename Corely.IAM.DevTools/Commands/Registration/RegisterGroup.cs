using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Registration;

internal partial class Registration : CommandBase
{
    internal class RegisterGroup : CommandBase
    {
        [Argument("Filepath to register group request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IRegistrationService _registrationService;
        private readonly IAuthenticationService _authenticationService;

        public RegisterGroup(
            IRegistrationService registrationService,
            IAuthenticationService authenticationService
        )
            : base("group", "Register a new group")
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
                    new RegisterGroupRequest("groupName", Guid.Empty)
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await RegisterGroupAsync();
            }
        }

        private async Task RegisterGroupAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var request = SampleJsonFileHelper.ReadMultipleRequestJson<RegisterGroupRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            foreach (var registerRequest in request)
            {
                var result = await _registrationService.RegisterGroupAsync(registerRequest);
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
