using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Registration;

internal partial class Registration : CommandBase
{
    internal class RegisterPermissionsWithRole : CommandBase
    {
        [Argument("Filepath to register permissions with role request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IRegistrationService _registrationService;
        private readonly IAuthenticationService _authenticationService;

        public RegisterPermissionsWithRole(
            IRegistrationService registrationService,
            IAuthenticationService authenticationService
        )
            : base("permissions-with-role", "Register permissions with role")
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
                    new RegisterPermissionsWithRoleRequest([Guid.Empty], Guid.Empty, Guid.Empty)
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await RegisterPermissionsWithRoleAsync();
            }
        }

        private async Task RegisterPermissionsWithRoleAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var request =
                SampleJsonFileHelper.ReadMultipleRequestJson<RegisterPermissionsWithRoleRequest>(
                    RequestJsonFile
                );
            if (request == null)
                return;

            foreach (var registerRequest in request)
            {
                var result = await _registrationService.RegisterPermissionsWithRoleAsync(
                    registerRequest
                );
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
