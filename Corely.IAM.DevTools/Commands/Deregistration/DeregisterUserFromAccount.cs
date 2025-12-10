using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterUserFromAccount : CommandBase
    {
        [Argument("Filepath to deregister user from account request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IDeregistrationService _deregistrationService;

        public DeregisterUserFromAccount(IDeregistrationService deregistrationService)
            : base("user-from-account", "Deregister a user from an account")
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
                    new DeregisterUserFromAccountRequest(1, 1)
                );
            }
            else
            {
                await DeregisterUserFromAccountAsync();
            }
        }

        private async Task DeregisterUserFromAccountAsync()
        {
            var request = SampleJsonFileHelper.ReadRequestJson<DeregisterUserFromAccountRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            try
            {
                foreach (var deregisterRequest in request)
                {
                    var result = await _deregistrationService.DeregisterUserFromAccountAsync(
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
