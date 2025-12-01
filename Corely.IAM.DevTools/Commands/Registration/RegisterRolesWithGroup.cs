using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Validators;

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

        public RegisterRolesWithGroup(IRegistrationService registrationService)
            : base("roles-with-group", "Register roles with group")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
        }

        protected override async Task ExecuteAsync()
        {
            if (Create)
            {
                CreateSampleJson(RequestJsonFile, new RegisterRolesWithGroupRequest([1, 2], 3));
            }
            else
            {
                await RegisterRolesWithGroupAsync();
            }
        }

        private async Task RegisterRolesWithGroupAsync()
        {
            var request = ReadRequestJson<RegisterRolesWithGroupRequest>(RequestJsonFile);
            if (request == null)
                return;
            try
            {
                foreach (var registerRequest in request)
                {
                    var result = await _registrationService.RegisterRolesWithGroupAsync(
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
