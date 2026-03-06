using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Invitation;

internal partial class Invitation : CommandBase
{
    internal class Accept : CommandBase
    {
        [Option("-t", "--token", Description = "Invitation token")]
        private string Token { get; init; } = null!;

        private readonly IRegistrationService _registrationService;
        private readonly IUserContextProvider _userContextProvider;

        public Accept(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("accept", "Accept an invitation")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                ShowHelp("--token is required");
                return;
            }

            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            try
            {
                var request = new AcceptInvitationRequest(Token);
                var result = await _registrationService.AcceptInvitationAsync(request);

                if (result.ResultCode == AcceptInvitationResultCode.Success)
                {
                    Success($"Invitation accepted successfully");
                    Info($"  Account ID: {result.AccountId}");
                }
                else
                {
                    Error($"{result.ResultCode}: {result.Message}");
                }
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }
    }
}
