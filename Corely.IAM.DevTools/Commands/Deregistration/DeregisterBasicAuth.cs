using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterBasicAuth : CommandBase
    {
        [Argument("The current password", true)]
        private string CurrentPassword { get; init; } = null!;

        [Argument("TOTP or recovery code, when TOTP is enabled", false)]
        private string? Code { get; init; }

        private readonly IDeregistrationService _deregistrationService;
        private readonly IAuthenticationService _authenticationService;

        public DeregisterBasicAuth(
            IDeregistrationService deregistrationService,
            IAuthenticationService authenticationService
        )
            : base("basic-auth", "Remove password authentication for the current user")
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

            var result = await _deregistrationService.DeregisterBasicAuthAsync(
                new DeregisterBasicAuthRequest(CurrentPassword, Code)
            );

            if (result.ResultCode == DeregisterBasicAuthResultCode.Success)
            {
                Success("Basic auth removed successfully.");
            }
            else
            {
                Error($"{result.ResultCode}: {result.Message}");
            }
        }
    }
}
