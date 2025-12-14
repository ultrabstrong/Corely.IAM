using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterRolesFromUser : CommandBase
    {
        [Argument("Filepath to deregister roles from user request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Argument("Filepath to auth token json", true)]
        private string AuthTokenFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IDeregistrationService _deregistrationService;
        private readonly IUserContextProvider _userContextProvider;

        public DeregisterRolesFromUser(
            IDeregistrationService deregistrationService,
            IUserContextProvider userContextProvider
        )
            : base("roles-from-user", "Deregister roles from a user")
        {
            _deregistrationService = deregistrationService.ThrowIfNull(
                nameof(deregistrationService)
            );
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (Create)
            {
                SampleJsonFileHelper.CreateSampleJson(
                    RequestJsonFile,
                    new DeregisterRolesFromUserRequest([1, 2, 3], 1)
                );
            }
            else
            {
                await DeregisterRolesFromUserAsync();
            }
        }

        private async Task DeregisterRolesFromUserAsync()
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

            var request = SampleJsonFileHelper.ReadRequestJson<DeregisterRolesFromUserRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            try
            {
                foreach (var deregisterRequest in request)
                {
                    var result = await _deregistrationService.DeregisterRolesFromUserAsync(
                        deregisterRequest
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
