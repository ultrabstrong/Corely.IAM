using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterPermission : CommandBase
    {
        [Argument("Filepath to deregister permission request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IDeregistrationService _deregistrationService;
        private readonly IAuthenticationService _authenticationService;

        public DeregisterPermission(
            IDeregistrationService deregistrationService,
            IAuthenticationService authenticationService
        )
            : base("permission", "Deregister a permission")
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
                    new DeregisterPermissionRequest(Guid.Empty, Guid.Empty)
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await DeregisterPermissionAsync();
            }
        }

        private async Task DeregisterPermissionAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var request = SampleJsonFileHelper.ReadMultipleRequestJson<DeregisterPermissionRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            foreach (var deregisterRequest in request)
            {
                var result = await _deregistrationService.DeregisterPermissionAsync(
                    deregisterRequest
                );
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
