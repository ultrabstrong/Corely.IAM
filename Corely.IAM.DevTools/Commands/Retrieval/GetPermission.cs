using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class GetPermission : CommandBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        [Argument("The permission ID (GUID)", true)]
        private string Id { get; init; } = null!;

        [Option("-h", "--hydrate", Description = "Hydrate related entities")]
        private bool Hydrate { get; init; } = false;

        private readonly IRetrievalService _retrievalService;
        private readonly IUserContextProvider _userContextProvider;

        public GetPermission(
            IRetrievalService retrievalService,
            IUserContextProvider userContextProvider
        )
            : base("get-permission", "Get a permission by ID")
        {
            _retrievalService = retrievalService.ThrowIfNull(nameof(retrievalService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            var result = await _retrievalService.GetPermissionAsync(Guid.Parse(Id), Hydrate);
            Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        }
    }
}
