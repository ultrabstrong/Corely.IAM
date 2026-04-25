using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class Enable : CommandBase
    {
        private readonly IMfaService _mfaService;
        private readonly IAuthenticationService _authenticationService;

        public Enable(IMfaService mfaService, IAuthenticationService authenticationService)
            : base("enable", "Enable TOTP setup for the current user")
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

            var result = await _mfaService.EnableTotpAsync();

            if (result.ResultCode == EnableTotpResultCode.Success)
            {
                Success("TOTP enabled successfully.");
                Info($"Secret: {result.Secret}");
                Info($"Setup URI: {result.SetupUri}");
                Info("Recovery codes:");
                foreach (var code in result.RecoveryCodes!)
                {
                    Info($"  {code}");
                }
            }
            else
            {
                Error($"Enable TOTP failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
