using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;

namespace Corely.IAM.DevTools.Commands.Modification;

internal partial class Modification : CommandBase
{
    internal class ModifyUser : CommandBase
    {
        [Argument("Filepath to update user request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IModificationService _modificationService;
        private readonly IAuthenticationService _authenticationService;

        public ModifyUser(
            IModificationService modificationService,
            IAuthenticationService authenticationService
        )
            : base("user", "Update a user")
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
                    new UpdateUserRequest(Guid.Empty, "username", "email@example.com")
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await ModifyUserAsync();
            }
        }

        private async Task ModifyUserAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var requests = SampleJsonFileHelper.ReadMultipleRequestJson<UpdateUserRequest>(
                RequestJsonFile
            );
            if (requests == null)
                return;

            foreach (var request in requests)
            {
                var result = await _modificationService.ModifyUserAsync(request);
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
