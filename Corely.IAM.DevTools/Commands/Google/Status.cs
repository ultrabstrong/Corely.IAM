using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Google;

internal partial class Google : CommandBase
{
    internal class Status : CommandBase
    {
        private readonly IRetrievalService _retrievalService;
        private readonly IUserContextProvider _userContextProvider;

        public Status(IRetrievalService retrievalService, IUserContextProvider userContextProvider)
            : base("status", "Show authentication method status for the current user")
        {
            _retrievalService = retrievalService.ThrowIfNull(nameof(retrievalService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _retrievalService.GetAuthMethodsAsync();

            Info($"Has basic auth: {result.HasBasicAuth}");
            Info($"Has Google auth: {result.HasGoogleAuth}");
            if (result.HasGoogleAuth)
            {
                Info($"Google email: {result.GoogleEmail}");
            }
        }
    }
}
