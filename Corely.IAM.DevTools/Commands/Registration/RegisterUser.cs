using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

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
                SampleJsonFileHelper.CreateSampleMultipleRequestJson(
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
            var request = SampleJsonFileHelper.ReadMultipleRequestJson<RegisterUserRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            foreach (var registerRequest in request)
            {
                var result = await _registrationService.RegisterUserAsync(registerRequest);
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
