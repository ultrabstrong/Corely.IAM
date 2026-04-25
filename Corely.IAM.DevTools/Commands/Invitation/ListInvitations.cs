using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Invitation;

internal partial class Invitation : CommandBase
{
    internal class ListInvitations : CommandBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        [Option("-a", "--account-id", Description = "Account ID (GUID)")]
        private string AccountId { get; init; } = null!;

        [Option("-s", "--skip", Description = "Number of records to skip")]
        private int Skip { get; init; } = 0;

        [Option("-t", "--take", Description = "Number of records to take")]
        private int Take { get; init; } = 20;

        private readonly IInvitationService _invitationService;
        private readonly IAuthenticationService _authenticationService;

        public ListInvitations(
            IInvitationService invitationService,
            IAuthenticationService authenticationService
        )
            : base("list", "List invitations for an account")
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

            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var result = await _invitationService.ListInvitationsAsync(
                new ListInvitationsRequest(Guid.Parse(AccountId), Skip: Skip, Take: Take)
            );

            Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        }
    }
}
