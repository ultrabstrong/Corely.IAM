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
    internal class DeregisterRole : CommandBase
    {
        [Argument("Filepath to deregister role request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Argument("Filepath to auth token json", true)]
        private string AuthTokenFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IDeregistrationService _deregistrationService;
        private readonly IUserContextProvider _userContextProvider;

        public DeregisterRole(
            IDeregistrationService deregistrationService,
            IUserContextProvider userContextProvider
        )
            : base("role", "Deregister a role")
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
                    new DeregisterRoleRequest(1)
                );
            }
            else
            {
                await DeregisterRoleAsync();
            }
        }

        private async Task DeregisterRoleAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(AuthTokenFile, _userContextProvider))
                return;

            var request = SampleJsonFileHelper.ReadRequestJson<DeregisterRoleRequest>(
                RequestJsonFile
            );
            if (request == null)
                return;

            try
            {
                foreach (var deregisterRequest in request)
                {
                    var result = await _deregistrationService.DeregisterRoleAsync(
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
