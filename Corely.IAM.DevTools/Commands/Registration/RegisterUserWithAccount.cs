using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using System.Text.Json;

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
        private readonly IUserContextProvider _userContextProvider;

        public RegisterUserWithAccount(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("user-with-account", "Register an existing user with an existing account")
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
                    new RegisterUserWithAccountRequest(Guid.Empty)
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await RegisterUserWithAccountAsync();
            }
        }

        private async Task RegisterUserWithAccountAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var request =
                SampleJsonFileHelper.ReadMultipleRequestJson<RegisterUserWithAccountRequest>(
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
