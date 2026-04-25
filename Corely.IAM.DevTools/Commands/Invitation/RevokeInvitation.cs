using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Invitation;

internal partial class Invitation : CommandBase
{
    internal class RevokeInvitation : CommandBase
    {
        [Option("-a", "--account-id", Description = "Account ID (GUID)")]
        private string AccountId { get; init; } = null!;

        [Option("-i", "--invitation-id", Description = "Invitation ID (GUID)")]
        private string InvitationId { get; init; } = null!;

        private readonly IInvitationService _invitationService;
        private readonly IAuthenticationService _authenticationService;

        public RevokeInvitation(
            IInvitationService invitationService,
            IAuthenticationService authenticationService
        )
            : base("revoke", "Revoke an invitation")
        {
            _invitationService = invitationService.ThrowIfNull(nameof(invitationService));
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(AccountId))
            {
                ShowHelp("--account-id is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(InvitationId))
            {
                ShowHelp("--invitation-id is required");
                return;
            }

            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var result = await _invitationService.RevokeInvitationAsync(
                new RevokeInvitationRequest(Guid.Parse(AccountId), Guid.Parse(InvitationId))
            );

            if (result.ResultCode == RevokeInvitationResultCode.Success)
            {
                Success("Invitation revoked successfully");
            }
            else
            {
                Error($"{result.ResultCode}: {result.Message}");
            }
        }
    }
}
