using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

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

        private readonly IRegistrationService _registrationService;
        private readonly IUserContextProvider _userContextProvider;

        public ListInvitations(
            IRegistrationService registrationService,
            IUserContextProvider userContextProvider
        )
            : base("list", "List invitations for an account")
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

            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _registrationService.ListInvitationsAsync(
                Guid.Parse(AccountId),
                skip: Skip,
                take: Take
            );

            Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        }
    }
}
