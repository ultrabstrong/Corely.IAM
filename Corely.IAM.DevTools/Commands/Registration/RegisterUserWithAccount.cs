using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Registration;

internal partial class Registration : CommandBase
{
    internal class RegisterUserWithAccount : CommandBase
    {
        [Argument("Filepath to register user with account request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IRegistrationService _registrationService;

        public RegisterUserWithAccount(IRegistrationService registrationService)
            : base("user-with-account", "Register an existing user with an existing account")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
        }

        protected override async Task ExecuteAsync()
        {
            if (Create)
            {
                SampleJsonFileHelper.CreateSampleJson(
                    RequestJsonFile,
                    new RegisterUserWithAccountRequest(1)
                );
            }
            else
            {
                await RegisterUserWithAccountAsync();
            }
        }

        private async Task RegisterUserWithAccountAsync()
        {
            var request = SampleJsonFileHelper.ReadRequestJson<RegisterUserWithAccountRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            try
            {
                foreach (var registerRequest in request)
                {
                    var result = await _registrationService.RegisterUserWithAccountAsync(
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
