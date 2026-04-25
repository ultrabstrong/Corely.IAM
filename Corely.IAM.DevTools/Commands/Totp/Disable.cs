using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class Disable : CommandBase
    {
        [Argument("The TOTP code to confirm disable", true)]
        private string Code { get; init; } = null!;

        private readonly IMfaService _mfaService;
        private readonly IAuthenticationService _authenticationService;

        public Disable(IMfaService mfaService, IAuthenticationService authenticationService)
            : base("disable", "Disable TOTP for the current user")
        {
            _mfaService = mfaService.ThrowIfNull(nameof(mfaService));
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var result = await _mfaService.DisableTotpAsync(new DisableTotpRequest(Code));

            if (result.ResultCode == DisableTotpResultCode.Success)
            {
                Success("TOTP disabled successfully.");
            }
            else
            {
                Error($"Disable TOTP failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
