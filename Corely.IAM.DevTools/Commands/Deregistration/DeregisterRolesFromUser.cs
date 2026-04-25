using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterRolesFromUser : CommandBase
    {
        [Argument("Filepath to deregister roles from user request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IDeregistrationService _deregistrationService;
        private readonly IAuthenticationService _authenticationService;

        public DeregisterRolesFromUser(
            IDeregistrationService deregistrationService,
            IAuthenticationService authenticationService
        )
            : base("roles-from-user", "Deregister roles from a user")
        {
            _deregistrationService = deregistrationService.ThrowIfNull(
                nameof(deregistrationService)
            );
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
                    new DeregisterRolesFromUserRequest([Guid.Empty], Guid.Empty, Guid.Empty)
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await DeregisterRolesFromUserAsync();
            }
        }

        private async Task DeregisterRolesFromUserAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var request =
                SampleJsonFileHelper.ReadMultipleRequestJson<DeregisterRolesFromUserRequest>(
                    RequestJsonFile
                );
            if (request == null)
                return;

            foreach (var deregisterRequest in request)
            {
                var result = await _deregistrationService.DeregisterRolesFromUserAsync(
                    deregisterRequest
                );
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
