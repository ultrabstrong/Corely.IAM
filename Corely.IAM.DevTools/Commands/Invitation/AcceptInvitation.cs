using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Invitation;

internal partial class Invitation : CommandBase
{
    internal class AcceptInvitation : CommandBase
    {
        [Option("-t", "--token", Description = "Invitation token")]
        private string Token { get; init; } = null!;

        private readonly IInvitationService _invitationService;
        private readonly IUserContextProvider _userContextProvider;

        public AcceptInvitation(
            IInvitationService invitationService,
            IUserContextProvider userContextProvider
        )
            : base("accept", "Accept an invitation")
        {
            _invitationService = invitationService.ThrowIfNull(nameof(invitationService));
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
