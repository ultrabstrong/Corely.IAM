using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class ListUsers : CommandBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        [Option("-a", "--account-id", Description = "Account ID (GUID)")]
        private string AccountId { get; init; } = null!;

        [Option("-s", "--skip", Description = "Number of records to skip")]
        private int Skip { get; init; } = 0;

        [Option("-t", "--take", Description = "Number of records to take")]
        private int Take { get; init; } = 25;

        private readonly IRetrievalService _retrievalService;
        private readonly IAuthenticationService _authenticationService;

        public ListUsers(
            IRetrievalService retrievalService,
            IAuthenticationService authenticationService
        )
            : base("list-users", "List users")
        {
            _retrievalService = retrievalService.ThrowIfNull(nameof(retrievalService));
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

            var result = await _retrievalService.ListUsersAsync(
                new ListUsersRequest(Guid.Parse(AccountId), Skip: Skip, Take: Take)
            );
            Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        }
    }
}
