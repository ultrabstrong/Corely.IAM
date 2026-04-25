using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Invitations.Constants;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Invitation;

internal partial class Invitation : CommandBase
{
    internal class CreateInvitation : CommandBase
    {
        [Option("-a", "--account-id", Description = "Account ID (GUID)")]
        private string AccountId { get; init; } = null!;

        [Option("-e", "--email", Description = "Email address for the invitation")]
        private string Email { get; init; } = null!;

        [Option("-d", "--description", Description = "Optional description")]
        private string? InvitationDescription { get; init; }

        [Option("-x", "--expires", Description = "Expiration in seconds")]
        private int ExpiresInSeconds { get; init; } = InvitationConstants.DEFAULT_EXPIRY_SECONDS;

        private readonly IInvitationService _invitationService;
        private readonly IAuthenticationService _authenticationService;

        public CreateInvitation(
            IInvitationService invitationService,
            IAuthenticationService authenticationService
        )
            : base("create", "Create a new invitation")
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

            if (string.IsNullOrWhiteSpace(Email))
            {
                ShowHelp("--email is required");
                return;
            }

            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var request = new CreateInvitationRequest(
                Guid.Parse(AccountId),
                Email,
                InvitationDescription,
                ExpiresInSeconds
            );

            var result = await _invitationService.CreateInvitationAsync(request);

            if (result.ResultCode == CreateInvitationResultCode.Success)
            {
                Success($"Invitation created successfully");
                Info($"  Invitation ID: {result.InvitationId}");
                Info($"  Token: {result.Token}");
            }
            else
            {
                Error($"{result.ResultCode}: {result.Message}");
            }
        }
    }
}
