using Corely.Common.Extensions;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Identity;

internal partial class Identity : CommandBase
{
    internal class Status : CommandBase
    {
        private readonly IFederatedIdentityService _federatedIdentityService;
        private readonly IAuthenticationService _authenticationService;

        public Status(
            IFederatedIdentityService federatedIdentityService,
            IAuthenticationService authenticationService
        )
            : base("status", "Show the sign in methods of the current user")
        {
            _federatedIdentityService = federatedIdentityService.ThrowIfNull(
                nameof(federatedIdentityService)
            );
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var result = await _federatedIdentityService.GetAuthMethodsAsync();

            Info($"Has password: {result.HasBasicAuth}");
            foreach (var identity in result.LinkedIdentities)
            {
                Info($"{identity.Type}: {identity.Email ?? "no email"}");
            }
        }
    }
}
