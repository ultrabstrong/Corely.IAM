using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class Status : CommandBase
    {
        private readonly IMfaService _mfaService;
        private readonly IAuthenticationService _authenticationService;

        public Status(IMfaService mfaService, IAuthenticationService authenticationService)
            : base("status", "Show TOTP status for the current user")
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

            var result = await _mfaService.GetTotpStatusAsync();

            if (result.ResultCode == TotpStatusResultCode.Success)
            {
                Info($"TOTP enabled: {result.IsEnabled}");
                Info($"Remaining recovery codes: {result.RemainingRecoveryCodes}");
            }
            else
            {
                Error($"Get TOTP status failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
