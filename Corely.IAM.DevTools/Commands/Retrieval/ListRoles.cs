using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class ListRoles : CommandBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        [Option("-s", "--skip", Description = "Number of records to skip")]
        private int Skip { get; init; } = 0;

        [Option("-t", "--take", Description = "Number of records to take")]
        private int Take { get; init; } = 25;

        private readonly IRetrievalService _retrievalService;
        private readonly IUserContextProvider _userContextProvider;

        public ListRoles(
            IRetrievalService retrievalService,
            IUserContextProvider userContextProvider
        )
            : base("list-roles", "List roles")
        {
            _retrievalService = retrievalService.ThrowIfNull(nameof(retrievalService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _retrievalService.ListRolesAsync(null, null, Skip, Take);
            Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        }
    }
}
