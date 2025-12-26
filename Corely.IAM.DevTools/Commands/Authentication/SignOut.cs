using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Authentication;

internal partial class Authentication : CommandBase
{
    internal class SignOut : CommandBase
    {
        [Argument("Token ID to revoke")]
        private string TokenId { get; init; } = null!;

        private readonly IAuthenticationService _authenticationService;
        private readonly IUserContextProvider _userContextProvider;

        public SignOut(
            IAuthenticationService authenticationService,
            IUserContextProvider userContextProvider
        )
            : base("signout", "Sign out a user by revoking a specific auth token")
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
                var request = new SignOutRequest(TokenId);
                var result = await _authenticationService.SignOutAsync(request);

                var context = _userContextProvider.GetUserContext();
                var output = new
                {
                    context?.User,
                    TokenId,
                    context?.DeviceId,
                    context?.CurrentAccount,
                    Success = result,
                };
                Console.WriteLine(JsonSerializer.Serialize(output));

                if (result)
                {
                    Success($"User {context?.User} signed out successfully");
                    ClearAuthTokenFile();
                }
                else
                {
                    Warn($"Failed to sign out with token {TokenId}");
                }
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }
    }
}
