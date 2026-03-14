using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Google;

internal partial class Google : CommandBase
{
    internal class Status : CommandBase
    {
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IUserContextProvider _userContextProvider;

        public Status(
            IGoogleAuthService googleAuthService,
            IUserContextProvider userContextProvider
        )
            : base("status", "Show authentication method status for the current user")
        {
            _googleAuthService = googleAuthService.ThrowIfNull(nameof(googleAuthService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _googleAuthService.GetAuthMethodsAsync();

            Info($"Has basic auth: {result.HasBasicAuth}");
            Info($"Has Google auth: {result.HasGoogleAuth}");
            if (result.HasGoogleAuth)
            {
                Info($"Google email: {result.GoogleEmail}");
            }
        }
    }
}
