using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Google;

internal partial class Google : CommandBase
{
    internal class Link : CommandBase
    {
        [Argument("Filepath to a file containing the Google ID token", true)]
        private string IdTokenFile { get; init; } = null!;

        private readonly IRegistrationService _registrationService;
        private readonly IUserContextProvider _userContextProvider;

        public Link(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("link", "Link a Google account to the current user")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            if (!FileExists(IdTokenFile))
                return;

            var idToken = (await File.ReadAllTextAsync(IdTokenFile)).Trim();
            var result = await _registrationService.LinkGoogleAuthAsync(
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
