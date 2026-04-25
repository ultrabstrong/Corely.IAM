using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterAccount : CommandBase
    {
        [Option("-a", "--account-id", Description = "Account ID (GUID)")]
        private string AccountId { get; init; } = null!;

        private readonly IDeregistrationService _deregistrationService;
        private readonly IAuthenticationService _authenticationService;

        public DeregisterAccount(
            IDeregistrationService deregistrationService,
            IAuthenticationService authenticationService
        )
            : base("account", "Deregister the currently signed-in account")
        {
            _deregistrationService = deregistrationService.ThrowIfNull(
                nameof(deregistrationService)
            );
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(AccountId))
            {
                ShowHelp("--account-id is required");
                return;
            }

            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var result = await _deregistrationService.DeregisterAccountAsync(
                new DeregisterAccountRequest(Guid.Parse(AccountId))
            );
            Console.WriteLine(JsonSerializer.Serialize(result));

            ClearAuthTokenFile();
        }
    }
}
