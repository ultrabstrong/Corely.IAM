using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Modification;

internal partial class Modification : CommandBase
{
    internal class ModifyAccount : CommandBase
    {
        [Argument("Filepath to update account request json", true)]
        private string RequestJsonFile { get; init; } = null!;

        [Option("-c", "--create", Description = "Create sample json file at path")]
        private bool Create { get; init; }

        private readonly IModificationService _modificationService;
        private readonly IAuthenticationService _authenticationService;

        public ModifyAccount(
            IModificationService modificationService,
            IAuthenticationService authenticationService
        )
            : base("account", "Update an account")
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
                    new UpdateAccountRequest(Guid.Empty, "accountName")
                );
            }
            else
            {
                if (!FileExists(RequestJsonFile))
                    return;

                await ModifyAccountAsync();
            }
        }

        private async Task ModifyAccountAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var requests = SampleJsonFileHelper.ReadMultipleRequestJson<UpdateAccountRequest>(
                RequestJsonFile
            );
            if (requests == null)
                return;

            foreach (var request in requests)
            {
                var result = await _modificationService.ModifyAccountAsync(request);
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }
    }
}
