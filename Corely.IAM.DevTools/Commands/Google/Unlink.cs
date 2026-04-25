using Corely.Common.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Google;

internal partial class Google : CommandBase
{
    internal class Unlink : CommandBase
    {
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IAuthenticationService _authenticationService;

        public Unlink(
            IGoogleAuthService googleAuthService,
            IAuthenticationService authenticationService
        )
            : base("unlink", "Unlink the Google account from the current user")
        {
            _googleAuthService = googleAuthService.ThrowIfNull(nameof(googleAuthService));
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var result = await _googleAuthService.UnlinkGoogleAuthAsync();

            if (result.ResultCode == UnlinkGoogleAuthResultCode.Success)
            {
                Success("Google account unlinked successfully.");
            }
            else
            {
                Error($"Unlink failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
