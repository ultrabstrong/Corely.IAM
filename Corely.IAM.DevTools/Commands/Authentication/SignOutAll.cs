using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Authentication;

internal partial class Authentication : CommandBase
{
    internal class SignOutAll : CommandBase
    {
        [Argument("User ID to sign out from all sessions")]
        private int UserId { get; init; }

        private readonly IAuthenticationService _authenticationService;

        public SignOutAll(IAuthenticationService authenticationService)
            : base("signout-all", "Sign out a user from all sessions by revoking all auth tokens")
        {
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            try
            {
                await _authenticationService.SignOutAllAsync(UserId);
                var output = new { UserId, Success = true };
                Console.WriteLine(JsonSerializer.Serialize(output));
                Success($"All sessions for user {UserId} signed out successfully");
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }
    }
}
