using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterAccount : CommandBase
    {
        private readonly IDeregistrationService _deregistrationService;
        private readonly IUserContextProvider _userContextProvider;

        public DeregisterAccount(
            IDeregistrationService deregistrationService,
            IUserContextProvider userContextProvider
        )
            : base("account", "Deregister the currently signed-in account")
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

            var result = await _deregistrationService.DeregisterAccountAsync();
            Console.WriteLine(JsonSerializer.Serialize(result));

            ClearAuthTokenFile();
        }
    }
}
