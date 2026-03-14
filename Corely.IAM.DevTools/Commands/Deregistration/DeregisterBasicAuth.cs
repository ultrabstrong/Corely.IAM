using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterBasicAuth : CommandBase
    {
        private readonly IDeregistrationService _deregistrationService;
        private readonly IUserContextProvider _userContextProvider;

        public DeregisterBasicAuth(
            IDeregistrationService deregistrationService,
            IUserContextProvider userContextProvider
        )
            : base("basic-auth", "Remove password authentication for the current user")
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

            var result = await _deregistrationService.DeregisterBasicAuthAsync();

            if (result.ResultCode == DeregisterBasicAuthResultCode.Success)
            {
                Success("Basic auth removed successfully.");
            }
            else
            {
                Error($"{result.ResultCode}: {result.Message}");
            }
        }
    }
}
