using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Registration;

internal partial class Registration : CommandBase
{
    internal class RegisterRolesWithGroup : CommandBase
    {
        [Argument("Filepath to register roles with group request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IRegistrationService _registrationService;
        private readonly IAuthenticationService _authenticationService;

        public RegisterRolesWithGroup(
            IRegistrationService registrationService,
            IAuthenticationService authenticationService
        )
            : base("roles-with-group", "Register roles with group")
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
                    new RegisterRolesWithGroupRequest([Guid.Empty], Guid.Empty, Guid.Empty)
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await RegisterRolesWithGroupAsync();
            }
        }

        private async Task RegisterRolesWithGroupAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var request =
                SampleJsonFileHelper.ReadMultipleRequestJson<RegisterRolesWithGroupRequest>(
                    RequestJsonFile
                );
            if (request == null)
                return;

            foreach (var registerRequest in request)
            {
                var result = await _registrationService.RegisterRolesWithGroupAsync(
                    registerRequest
                );
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
