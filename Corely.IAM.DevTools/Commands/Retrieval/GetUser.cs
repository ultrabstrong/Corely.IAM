using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class GetUser : CommandBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        [Argument("The user ID (GUID)", true)]
        private string Id { get; init; } = null!;

        [Option("-h", "--hydrate", Description = "Hydrate related entities")]
        private bool Hydrate { get; init; } = false;

        private readonly IRetrievalService _retrievalService;
        private readonly IAuthenticationService _authenticationService;

        public GetUser(
            IRetrievalService retrievalService,
            IAuthenticationService authenticationService
        )
            : base("get-user", "Get a user by ID")
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

            var result = await _retrievalService.GetUserAsync(Guid.Parse(Id), Hydrate);
            Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        }
    }
}
