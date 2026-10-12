using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Password;

internal partial class Password : CommandBase
{
    internal class Register : CommandBase
    {
        [Argument("The new password", true)]
        private string NewPassword { get; init; } = null!;

        [Argument("TOTP or recovery code, when TOTP is enabled", false)]
        private string? Code { get; init; }

        private readonly IRegistrationService _registrationService;
        private readonly IAuthenticationService _authenticationService;

        public Register(
            IRegistrationService registrationService,
            IAuthenticationService authenticationService
        )
            : base("register", "Add a password for the current user")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var result = await _registrationService.RegisterPasswordAsync(
                new RegisterPasswordRequest(NewPassword, Code)
            );

            if (result.ResultCode == RegisterPasswordResultCode.Success)
            {
                Success("Password registered successfully.");
            }
            else
            {
                Error($"Register password failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
