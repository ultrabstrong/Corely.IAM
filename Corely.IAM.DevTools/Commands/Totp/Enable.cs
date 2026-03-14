using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class Enable : CommandBase
    {
        private readonly IMfaService _mfaService;
        private readonly IUserContextProvider _userContextProvider;

        public Enable(IMfaService mfaService, IUserContextProvider userContextProvider)
            : base("enable", "Enable TOTP setup for the current user")
        {
            _mfaService = mfaService.ThrowIfNull(nameof(mfaService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
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
