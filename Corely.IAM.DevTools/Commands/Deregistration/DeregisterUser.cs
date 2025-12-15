using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterUser : CommandBase
    {
        [Argument("Filepath to auth token json", true)]
        private string AuthTokenFile { get; init; } = null!;

        private readonly IDeregistrationService _deregistrationService;
        private readonly IUserContextProvider _userContextProvider;

        public DeregisterUser(
            IDeregistrationService deregistrationService,
            IUserContextProvider userContextProvider
        )
            : base("user", "Deregister a user")
        {
            _deregistrationService = deregistrationService.ThrowIfNull(
                nameof(deregistrationService)
            );
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(AuthTokenFile, _userContextProvider))
                return;

            try
            {
                var result = await _deregistrationService.DeregisterUserAsync();
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }
    }
}
