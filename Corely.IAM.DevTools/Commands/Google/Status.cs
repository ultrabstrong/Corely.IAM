using Corely.Common.Extensions;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Google;

internal partial class Google : CommandBase
{
    internal class Status : CommandBase
    {
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IAuthenticationService _authenticationService;

        public Status(
            IGoogleAuthService googleAuthService,
            IAuthenticationService authenticationService
        )
            : base("status", "Show authentication method status for the current user")
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
