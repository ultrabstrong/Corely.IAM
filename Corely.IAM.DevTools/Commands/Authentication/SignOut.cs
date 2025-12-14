using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
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

        private readonly IAuthenticationService _authenticationService;

        public SignOut(IAuthenticationService authenticationService)
            : base("signout", "Sign out a user by revoking a specific auth token")
        {
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            try
            {
                var result = await _authenticationService.SignOutAsync(UserId, TokenId);
                var output = new
                {
                    UserId,
                    TokenId,
                    Success = result,
                };
                Console.WriteLine(JsonSerializer.Serialize(output));

                if (result)
                {
                    Success($"User {UserId} signed out successfully");
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
