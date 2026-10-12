using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Password;

internal partial class Password : CommandBase
{
    internal class Modify : CommandBase
    {
        [Argument("The current password", true)]
        private string CurrentPassword { get; init; } = null!;

        [Argument("The new password", true)]
        private string NewPassword { get; init; } = null!;

        [Argument("TOTP or recovery code, when TOTP is enabled", false)]
        private string? Code { get; init; }

        private readonly IModificationService _modificationService;
        private readonly IAuthenticationService _authenticationService;

        public Modify(
            IModificationService modificationService,
            IAuthenticationService authenticationService
        )
            : base("modify", "Change the current user's password")
        {
            _modificationService = modificationService.ThrowIfNull(nameof(modificationService));
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var result = await _modificationService.ModifyPasswordAsync(
                new ModifyPasswordRequest(CurrentPassword, NewPassword, Code)
            );

            if (result.ResultCode == ModifyPasswordResultCode.Success)
            {
                Success("Password modified successfully.");
            }
            else
            {
                Error($"Modify password failed: {result.ResultCode} - {result.Message}");
            }
        }
    }
}
