using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class RegenerateCodes : CommandBase
    {
        private readonly IMfaService _mfaService;
        private readonly IAuthenticationService _authenticationService;

        public RegenerateCodes(IMfaService mfaService, IAuthenticationService authenticationService)
            : base("regenerate-codes", "Regenerate TOTP recovery codes")
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

            var result = await _mfaService.RegenerateTotpRecoveryCodesAsync();

            if (result.ResultCode == RegenerateTotpRecoveryCodesResultCode.Success)
            {
                Success("Recovery codes regenerated successfully.");
                Info("New recovery codes:");
                foreach (var code in result.RecoveryCodes!)
                {
                    Info($"  {code}");
                }
            }
            else
            {
                Error($"Regenerate recovery codes failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
