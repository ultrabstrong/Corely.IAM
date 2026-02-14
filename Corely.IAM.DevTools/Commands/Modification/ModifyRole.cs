using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

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
        private readonly IUserContextProvider _userContextProvider;

        public ModifyRole(
            IModificationService modificationService,
            IUserContextProvider userContextProvider
        )
            : base("role", "Update a role")
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
                    new UpdateRoleRequest(Guid.Empty, "roleName", "description")
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
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var requests = SampleJsonFileHelper.ReadMultipleRequestJson<UpdateRoleRequest>(
                RequestJsonFile
            );
            if (requests == null)
                return;

            try
            {
                foreach (var request in requests)
                {
                    var result = await _modificationService.ModifyRoleAsync(request);
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
