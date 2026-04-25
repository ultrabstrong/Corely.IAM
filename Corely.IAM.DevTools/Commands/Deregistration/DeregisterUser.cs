using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterUser : CommandBase
    {
        private readonly IDeregistrationService _deregistrationService;
        private readonly IAuthenticationService _authenticationService;

        public DeregisterUser(
            IDeregistrationService deregistrationService,
            IAuthenticationService authenticationService
        )
            : base("user", "Deregister a user")
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
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var result = await _deregistrationService.DeregisterUserAsync();
            Console.WriteLine(JsonSerializer.Serialize(result));
            ClearAuthTokenFile();
        }
    }
}
