using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class GetGroup : CommandBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        [Argument("The group ID (GUID)", true)]
        private string Id { get; init; } = null!;

        [Option("-h", "--hydrate", Description = "Hydrate related entities")]
        private bool Hydrate { get; init; } = false;

        private readonly IRetrievalService _retrievalService;
        private readonly IUserContextProvider _userContextProvider;

        public GetGroup(
            IRetrievalService retrievalService,
            IUserContextProvider userContextProvider
        )
            : base("get-group", "Get a group by ID")
        {
            _retrievalService = retrievalService.ThrowIfNull(nameof(retrievalService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _retrievalService.GetGroupAsync(Guid.Parse(Id), Hydrate);
            Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        }
    }
}
