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
    internal class DeregisterPermissionsFromRole : CommandBase
    {
        [Argument("Filepath to deregister permissions from role request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IDeregistrationService _deregistrationService;
        private readonly IUserContextProvider _userContextProvider;

        public DeregisterPermissionsFromRole(
            IDeregistrationService deregistrationService,
            IUserContextProvider userContextProvider
        )
            : base("permissions-from-role", "Deregister permissions from a role")
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
                    new DeregisterPermissionsFromRoleRequest([Guid.Empty], Guid.Empty)
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await DeregisterPermissionsFromRoleAsync();
            }
        }

        private async Task DeregisterPermissionsFromRoleAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var request =
                SampleJsonFileHelper.ReadMultipleRequestJson<DeregisterPermissionsFromRoleRequest>(
                    RequestJsonFile
                );
            if (request == null)
                return;

            try
            {
                foreach (var deregisterRequest in request)
                {
                    var result = await _deregistrationService.DeregisterPermissionsFromRoleAsync(
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
