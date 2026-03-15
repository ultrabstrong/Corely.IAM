using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Invitation;

internal partial class Invitation : CommandBase
{
    internal class RevokeInvitation : CommandBase
    {
        [Option("-i", "--invitation-id", Description = "Invitation ID (GUID)")]
        private string InvitationId { get; init; } = null!;

        private readonly IInvitationService _invitationService;
        private readonly IUserContextProvider _userContextProvider;

        public RevokeInvitation(
            IInvitationService invitationService,
            IUserContextProvider userContextProvider
        )
            : base("revoke", "Revoke an invitation")
        {
            _invitationService = invitationService.ThrowIfNull(nameof(invitationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(InvitationId))
            {
                ShowHelp("--invitation-id is required");
                return;
            }

            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _invitationService.RevokeInvitationAsync(Guid.Parse(InvitationId));

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
