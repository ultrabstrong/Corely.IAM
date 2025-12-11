using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
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

        public DeregisterPermissionsFromRole(IDeregistrationService deregistrationService)
            : base("permissions-from-role", "Deregister permissions from a role")
        {
            _deregistrationService = deregistrationService.ThrowIfNull(
                nameof(deregistrationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (Create)
            {
                SampleJsonFileHelper.CreateSampleJson(
                    RequestJsonFile,
                    new DeregisterPermissionsFromRoleRequest([1, 2, 3], 1)
                );
            }
            else
            {
                await DeregisterPermissionsFromRoleAsync();
            }
        }

        private async Task DeregisterPermissionsFromRoleAsync()
        {
            var request =
                SampleJsonFileHelper.ReadRequestJson<DeregisterPermissionsFromRoleRequest>(
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
