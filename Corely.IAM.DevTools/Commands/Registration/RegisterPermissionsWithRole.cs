using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

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
        private readonly IUserContextProvider _userContextProvider;

        public RegisterPermissionsWithRole(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("permissions-with-role", "Register permissions with role")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (Create)
            {
                SampleJsonFileHelper.CreateSampleMultipleRequestJson(
                    RequestJsonFile,
                    new RegisterPermissionsWithRoleRequest([Guid.Empty], Guid.Empty)
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
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var request =
                SampleJsonFileHelper.ReadMultipleRequestJson<RegisterPermissionsWithRoleRequest>(
                    RequestJsonFile
                );
            if (request == null)
                return;

            try
            {
                foreach (var registerRequest in request)
                {
                    var result = await _registrationService.RegisterPermissionsWithRoleAsync(
                        registerRequest
                    );
                    Console.WriteLine(JsonSerializer.Serialize(result));
                }
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }
    }
}
