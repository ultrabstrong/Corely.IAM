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
    internal class DeregisterRolesFromGroup : CommandBase
    {
        [Argument("Filepath to deregister roles from group request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IDeregistrationService _deregistrationService;
        private readonly IUserContextProvider _userContextProvider;

        public DeregisterRolesFromGroup(
            IDeregistrationService deregistrationService,
            IUserContextProvider userContextProvider
        )
            : base("roles-from-group", "Deregister roles from a group")
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
                    new DeregisterRolesFromGroupRequest([Guid.Empty], Guid.Empty)
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await DeregisterRolesFromGroupAsync();
            }
        }

        private async Task DeregisterRolesFromGroupAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var request =
                SampleJsonFileHelper.ReadMultipleRequestJson<DeregisterRolesFromGroupRequest>(
                    RequestJsonFile
                );
            if (request == null)
                return;

            try
            {
                foreach (var deregisterRequest in request)
                {
                    var result = await _deregistrationService.DeregisterRolesFromGroupAsync(
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
