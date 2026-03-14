using Corely.Common.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Google;

internal partial class Google : CommandBase
{
    internal class Unlink : CommandBase
    {
        private readonly IDeregistrationService _deregistrationService;
        private readonly IUserContextProvider _userContextProvider;

        public Unlink(
            IDeregistrationService deregistrationService,
            IUserContextProvider userContextProvider
        )
            : base("unlink", "Unlink the Google account from the current user")
        {
            _deregistrationService = deregistrationService.ThrowIfNull(
                nameof(deregistrationService)
            );
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _deregistrationService.UnlinkGoogleAuthAsync();

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
