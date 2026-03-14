using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class Enable : CommandBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly IUserContextProvider _userContextProvider;

        public Enable(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("enable", "Enable TOTP setup for the current user")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _registrationService.EnableTotpAsync();

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
