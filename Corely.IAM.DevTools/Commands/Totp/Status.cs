using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class Status : CommandBase
    {
        private readonly IRetrievalService _retrievalService;
        private readonly IUserContextProvider _userContextProvider;

        public Status(IRetrievalService retrievalService, IUserContextProvider userContextProvider)
            : base("status", "Show TOTP status for the current user")
        {
            _retrievalService = retrievalService.ThrowIfNull(nameof(retrievalService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _retrievalService.GetTotpStatusAsync();

            if (result.ResultCode == TotpStatusResultCode.Success)
            {
                Info($"TOTP enabled: {result.IsEnabled}");
                Info($"Remaining recovery codes: {result.RemainingRecoveryCodes}");
            }
            else
            {
                Error($"Get TOTP status failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
