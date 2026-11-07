using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Identity;

internal partial class Identity : CommandBase
{
    internal class Unlink : CommandBase
    {
        [Argument(
            "The identity provider to unlink: Google, Microsoft, GitHub, Facebook or Amazon",
            true
        )]
        private string Provider { get; init; } = null!;

        private readonly IFederatedIdentityService _federatedIdentityService;
        private readonly IAuthenticationService _authenticationService;

        public Unlink(
            IFederatedIdentityService federatedIdentityService,
            IAuthenticationService authenticationService
        )
            : base("unlink", "Unlink an identity provider account from the current user")
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

            if (!Enum.TryParse<IdentityProviderType>(Provider, true, out var type))
            {
                Error($"Unknown identity provider: {Provider}");
                return;
            }

            var result = await _federatedIdentityService.UnlinkFederatedIdentityAsync(type);
            if (result.ResultCode == UnlinkFederatedIdentityResultCode.Success)
            {
                Success($"{type} account unlinked.");
            }
            else
            {
                Error($"Unlink failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
