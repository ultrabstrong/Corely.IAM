using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterGroup : CommandBase
    {
        [Argument("Filepath to deregister group request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IDeregistrationService _deregistrationService;
        private readonly IAuthenticationService _authenticationService;

        public DeregisterGroup(
            IDeregistrationService deregistrationService,
            IAuthenticationService authenticationService
        )
            : base("group", "Deregister a group")
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
                    new DeregisterGroupRequest(Guid.Empty, Guid.Empty)
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await DeregisterGroupAsync();
            }
        }

        private async Task DeregisterGroupAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var request = SampleJsonFileHelper.ReadMultipleRequestJson<DeregisterGroupRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            foreach (var deregisterRequest in request)
            {
                var result = await _deregistrationService.DeregisterGroupAsync(deregisterRequest);
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
