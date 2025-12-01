using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Validators;
using Corely.Security.Password;

namespace Corely.IAM.DevTools.Commands.Registration;

internal partial class Registration : CommandBase
{
    internal class RegisterUser : CommandBase
    {
        [Argument("Filepath to register user request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IRegistrationService _registrationService;

        public RegisterUser(IRegistrationService registrationService)
            : base("user", "Register a new user")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
        }

        protected override async Task ExecuteAsync()
        {
            if (Create)
            {
                CreateSampleJson(
                    RequestJsonFile,
                    new RegisterUserRequest("userName", "email", "password")
                );
            }
            else
            {
                await RegisterUserAsync();
            }
        }

        private async Task RegisterUserAsync()
        {
            var request = ReadRequestJson<RegisterUserRequest>(RequestJsonFile);
            if (request == null)
                return;

            try
            {
                foreach (var registerRequest in request)
                {
                    var result = await _registrationService.RegisterUserAsync(registerRequest);
                    Console.WriteLine(JsonSerializer.Serialize(result));
                }
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
            catch (PasswordValidationException ex)
            {
                Error(ex.PasswordValidationResult.ValidationFailures);
            }
        }
    }
}
