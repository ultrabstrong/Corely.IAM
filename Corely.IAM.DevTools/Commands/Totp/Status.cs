using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class Status : CommandBase
    {
        private readonly IMfaService _mfaService;
        private readonly IUserContextProvider _userContextProvider;

        public Status(IMfaService mfaService, IUserContextProvider userContextProvider)
            : base("status", "Show TOTP status for the current user")
        {
            _mfaService = mfaService.ThrowIfNull(nameof(mfaService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
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
