using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

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
        private readonly IUserContextProvider _userContextProvider;

        public DeregisterPermission(
            IDeregistrationService deregistrationService,
            IUserContextProvider userContextProvider
        )
            : base("permission", "Deregister a permission")
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
                SampleJsonFileHelper.CreateSampleMultipleRequestJson(
                    RequestJsonFile,
                    new DeregisterPermissionRequest(Guid.Empty)
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
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var request = SampleJsonFileHelper.ReadMultipleRequestJson<DeregisterPermissionRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            try
            {
                foreach (var deregisterRequest in request)
                {
                    var result = await _deregistrationService.DeregisterPermissionAsync(
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
