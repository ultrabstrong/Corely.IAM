using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Registration;

internal partial class Registration : CommandBase
{
    internal class RegisterRolesWithUser : CommandBase
    {
        [Argument("Filepath to register roles with user request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Argument("Filepath to auth token json", true)]
        private string AuthTokenFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IRegistrationService _registrationService;
        private readonly IUserContextProvider _userContextProvider;

        public RegisterRolesWithUser(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("roles-with-user", "Register roles with user")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (Create)
            {
                SampleJsonFileHelper.CreateSampleJson(
                    RequestJsonFile,
                    new RegisterRolesWithUserRequest([1, 2], 3)
                );
            }
            else
            {
                await RegisterRolesWithUserAsync();
            }
        }

        private async Task RegisterRolesWithUserAsync()
        {
            var authToken = await LoadAuthTokenFromFileAsync(AuthTokenFile);
            if (authToken == null)
                return;

            var setContextResult = await _userContextProvider.SetUserContextAsync(authToken);
            if (setContextResult != UserAuthTokenValidationResultCode.Success)
            {
                Error($"Failed to set user context: {setContextResult}");
                return;
            }

            var request = SampleJsonFileHelper.ReadRequestJson<RegisterRolesWithUserRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            try
            {
                foreach (var registerRequest in request)
                {
                    var result = await _registrationService.RegisterRolesWithUserAsync(
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
