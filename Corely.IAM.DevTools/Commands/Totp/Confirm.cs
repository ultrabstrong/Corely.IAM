using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class Confirm : CommandBase
    {
        [Argument("The TOTP code to confirm setup", true)]
        private string Code { get; init; } = null!;

        private readonly IRegistrationService _registrationService;
        private readonly IUserContextProvider _userContextProvider;

        public Confirm(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("confirm", "Confirm TOTP setup with a code")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _registrationService.ConfirmTotpAsync(new ConfirmTotpRequest(Code));

            if (result.ResultCode == ConfirmTotpResultCode.Success)
            {
                Success("TOTP confirmed successfully.");
            }
            else
            {
                Error($"Confirm TOTP failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
