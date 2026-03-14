using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class RegenerateCodes : CommandBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly IUserContextProvider _userContextProvider;

        public RegenerateCodes(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("regenerate-codes", "Regenerate TOTP recovery codes")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _registrationService.RegenerateTotpRecoveryCodesAsync();

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
