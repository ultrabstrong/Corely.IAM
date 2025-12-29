using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

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
            try
            {
                var currentContext = _userContextProvider.GetUserContext();

                await _authenticationService.SignOutAllAsync();

                var output = new { Success = true };
                Console.WriteLine(JsonSerializer.Serialize(output));
                Success($"All sessions for user {currentContext!.User.Id} signed out successfully");

                ClearAuthTokenFile();
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }
    }
}
