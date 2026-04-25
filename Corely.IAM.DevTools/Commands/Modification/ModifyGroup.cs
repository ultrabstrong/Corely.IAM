using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Groups.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Modification;

internal partial class Modification : CommandBase
{
    internal class ModifyGroup : CommandBase
    {
        [Argument("Filepath to update group request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IModificationService _modificationService;
        private readonly IAuthenticationService _authenticationService;

        public ModifyGroup(
            IModificationService modificationService,
            IAuthenticationService authenticationService
        )
            : base("group", "Update a group")
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
                    new UpdateGroupRequest(Guid.Empty, Guid.Empty, "groupName", "description")
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await ModifyGroupAsync();
            }
        }

        private async Task ModifyGroupAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var requests = SampleJsonFileHelper.ReadMultipleRequestJson<UpdateGroupRequest>(
                RequestJsonFile
            );
            if (requests == null)
                return;

            foreach (var request in requests)
            {
                var result = await _modificationService.ModifyGroupAsync(request);
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
