using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Registration;

internal partial class Registration : CommandBase
{
    internal class RegisterUserWithGoogle : CommandBase
    {
        [Argument("Filepath to Google ID token file", true)]
        private string IdTokenFile { get; init; } = null!;

        private readonly IRegistrationService _registrationService;

        public RegisterUserWithGoogle(IRegistrationService registrationService)
            : base("user-with-google", "Register a new user from a Google ID token")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
        }

        protected override async Task ExecuteAsync()
        {
            if (!FileExists(IdTokenFile))
                return;

            var idToken = (await File.ReadAllTextAsync(IdTokenFile)).Trim();
            if (string.IsNullOrWhiteSpace(idToken))
            {
                Error("ID token file is empty.");
                return;
            }

            var result = await _registrationService.RegisterUserWithGoogleAsync(
                new RegisterUserWithGoogleRequest(idToken)
            );

            if (result.ResultCode == RegisterUserWithGoogleResultCode.Success)
            {
                Success($"User created: {result.CreatedUserId}");
            }
            else
            {
                Error($"{result.ResultCode}: {result.Message}");
            }
        }
    }
}
