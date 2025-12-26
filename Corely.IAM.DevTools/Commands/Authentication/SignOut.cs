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
        [Argument("User ID to sign out")]
        private int UserId { get; init; }

        [Argument("Token ID to revoke")]
        private string TokenId { get; init; } = null!;

        [Argument("Device ID")]
        private string DeviceId { get; init; } = null!;

        [Argument("Account ID (optional)", false)]
        private int? AccountId { get; init; }

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
                var request = new SignOutRequest(UserId, TokenId, DeviceId, AccountId);
                var result = await _authenticationService.SignOutAsync(request);
                var output = new
                {
                    UserId,
                    TokenId,
                    DeviceId,
                    AccountId,
                    Success = result,
                };
                Console.WriteLine(JsonSerializer.Serialize(output));

                if (result)
                {
                    Success($"User {UserId} signed out successfully");

                    var currentContext = _userContextProvider.GetUserContext();
                    if (currentContext?.UserId == UserId)
                    {
                        ClearAuthTokenFile();
                    }
                }
                else
                {
                    Warn($"Failed to sign out user {UserId} with token {TokenId}");
                }
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }
    }
}
