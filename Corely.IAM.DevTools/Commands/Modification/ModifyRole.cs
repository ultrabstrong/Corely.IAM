using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Modification;

internal partial class Modification : CommandBase
{
    internal class ModifyRole : CommandBase
    {
        [Argument("Filepath to update role request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IModificationService _modificationService;
        private readonly IAuthenticationService _authenticationService;

        public ModifyRole(
            IModificationService modificationService,
            IAuthenticationService authenticationService
        )
            : base("role", "Update a role")
        {
            _modificationService = modificationService.ThrowIfNull(nameof(modificationService));
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
                    new UpdateRoleRequest(Guid.Empty, Guid.Empty, "roleName", "description")
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await ModifyRoleAsync();
            }
        }

        private async Task ModifyRoleAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var requests = SampleJsonFileHelper.ReadMultipleRequestJson<UpdateRoleRequest>(
                RequestJsonFile
            );
            if (requests == null)
                return;

            foreach (var request in requests)
            {
                var result = await _modificationService.ModifyRoleAsync(request);
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
