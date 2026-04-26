using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Authentication;

internal partial class Authentication : CommandBase
{
    internal class SignOutAll : CommandBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IUserContextProvider _userContextProvider;

        public SignOutAll(
            IAuthenticationService authenticationService,
            IUserContextProvider userContextProvider
        )
            : base("signout-all", "Sign out a user from all sessions by revoking all auth tokens")
        {
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            var currentContext = _userContextProvider.GetUserContext();
            var userId =
                currentContext?.User?.Id
                ?? throw new InvalidOperationException(
                    "A non-system user context is required to sign out all sessions."
                );

            await _authenticationService.SignOutAllAsync();

            var output = new { Success = true };
            Console.WriteLine(JsonSerializer.Serialize(output));
            Success($"All sessions for user {userId} signed out successfully");

            ClearAuthTokenFile();
        }
    }
}
