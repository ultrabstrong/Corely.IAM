using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Invitation;

internal partial class Invitation : CommandBase
{
    internal class AcceptInvitation : CommandBase
    {
        [Option("-t", "--token", Description = "Invitation token")]
        private string Token { get; init; } = null!;

        private readonly IInvitationService _invitationService;
        private readonly IAuthenticationService _authenticationService;

        public AcceptInvitation(
            IInvitationService invitationService,
            IAuthenticationService authenticationService
        )
            : base("accept", "Accept an invitation")
        {
            _invitationService = invitationService.ThrowIfNull(nameof(invitationService));
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                ShowHelp("--token is required");
                return;
            }

            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var request = new AcceptInvitationRequest(Token);
            var result = await _invitationService.AcceptInvitationAsync(request);

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
    }
}
