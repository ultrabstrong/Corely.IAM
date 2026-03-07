using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Invitations.Constants;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

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

        private readonly IRegistrationService _registrationService;
        private readonly IUserContextProvider _userContextProvider;

        public CreateInvitation(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("create", "Create a new invitation")
        {
            _registrationService = registrationService.ThrowIfNull(nameof(registrationService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
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

            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            try
            {
                var request = new CreateInvitationRequest(
                    Guid.Parse(AccountId),
                    Email,
                    InvitationDescription,
                    ExpiresInSeconds
                );

                var result = await _registrationService.CreateInvitationAsync(request);

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
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }
    }
}
