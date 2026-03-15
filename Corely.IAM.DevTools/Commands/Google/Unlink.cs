using Corely.Common.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Google;

internal partial class Google : CommandBase
{
    internal class Unlink : CommandBase
    {
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IUserContextProvider _userContextProvider;

        public Unlink(
            IGoogleAuthService googleAuthService,
            IUserContextProvider userContextProvider
        )
            : base("unlink", "Unlink the Google account from the current user")
        {
            _googleAuthService = googleAuthService.ThrowIfNull(nameof(googleAuthService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
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
