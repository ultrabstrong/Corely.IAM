using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class Disable : CommandBase
    {
        [Argument("The TOTP code to confirm disable", true)]
        private string Code { get; init; } = null!;

        private readonly IRegistrationService _registrationService;
        private readonly IUserContextProvider _userContextProvider;

        public Disable(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("disable", "Disable TOTP for the current user")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _registrationService.DisableTotpAsync(new DisableTotpRequest(Code));

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
