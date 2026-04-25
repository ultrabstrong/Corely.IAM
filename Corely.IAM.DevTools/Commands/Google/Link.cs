using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Google;

internal partial class Google : CommandBase
{
    internal class Link : CommandBase
    {
        [Argument("Filepath to a file containing the Google ID token", true)]
        private string IdTokenFile { get; init; } = null!;

        private readonly IGoogleAuthService _googleAuthService;
        private readonly IAuthenticationService _authenticationService;

        public Link(
            IGoogleAuthService googleAuthService,
            IAuthenticationService authenticationService
        )
            : base("link", "Link a Google account to the current user")
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

            if (!FileExists(IdTokenFile))
                return;

            var idToken = (await File.ReadAllTextAsync(IdTokenFile)).Trim();
            var result = await _googleAuthService.LinkGoogleAuthAsync(
                new LinkGoogleAuthRequest(idToken)
            );

            if (result.ResultCode == LinkGoogleAuthResultCode.Success)
            {
                Success("Google account linked successfully.");
            }
            else
            {
                Error($"Link failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
