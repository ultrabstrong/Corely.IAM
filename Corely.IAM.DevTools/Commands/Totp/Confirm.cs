using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class Confirm : CommandBase
    {
        [Argument("The TOTP code to confirm setup", true)]
        private string Code { get; init; } = null!;

        private readonly IMfaService _mfaService;
        private readonly IAuthenticationService _authenticationService;

        public Confirm(IMfaService mfaService, IAuthenticationService authenticationService)
            : base("confirm", "Confirm TOTP setup with a code")
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

            var result = await _mfaService.ConfirmTotpAsync(new ConfirmTotpRequest(Code));

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
