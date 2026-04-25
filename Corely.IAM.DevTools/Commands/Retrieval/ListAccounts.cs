using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class ListAccounts : CommandBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        [Option("-s", "--skip", Description = "Number of records to skip")]
        private int Skip { get; init; } = 0;

        [Option("-t", "--take", Description = "Number of records to take")]
        private int Take { get; init; } = 25;

        private readonly IRetrievalService _retrievalService;
        private readonly IAuthenticationService _authenticationService;

        public ListAccounts(
            IRetrievalService retrievalService,
            IAuthenticationService authenticationService
        )
            : base("list-accounts", "List accounts")
        {
            _retrievalService = retrievalService.ThrowIfNull(nameof(retrievalService));
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            var result = await _retrievalService.ListAccountsAsync(
                new ListAccountsRequest(Skip: Skip, Take: Take)
            );
            Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        }
    }
}
