using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class ListRoles : CommandBase
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

        public ListRoles(
            IRetrievalService retrievalService,
            IAuthenticationService authenticationService
        )
            : base("list-roles", "List roles")
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

            var result = await _retrievalService.ListRolesAsync(
                new ListRolesRequest(Guid.Parse(AccountId), Skip: Skip, Take: Take)
            );
            Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        }
    }
}
