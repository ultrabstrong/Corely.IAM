using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

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
        private readonly IUserContextProvider _userContextProvider;

        public ModifyUser(
            IModificationService modificationService,
            IUserContextProvider userContextProvider
        )
            : base("user", "Update a user")
        {
            _modificationService = modificationService.ThrowIfNull(nameof(modificationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
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
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var requests = SampleJsonFileHelper.ReadMultipleRequestJson<UpdateUserRequest>(
                RequestJsonFile
            );
            if (requests == null)
                return;

            try
            {
                foreach (var request in requests)
                {
                    var result = await _modificationService.ModifyUserAsync(request);
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
