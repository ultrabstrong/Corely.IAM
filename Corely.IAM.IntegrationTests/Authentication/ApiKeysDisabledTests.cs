using Corely.IAM.ApiKeys.Models;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authentication;

public class ApiKeysDisabledTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task ByDefault_NoKeyCanBeCreated()
    {
        var created = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRegistrationService>()
                    .RegisterApiKeyAsync(new RegisterApiKeyRequest("ci", _scenario.AccountId))
        );

        Assert.Equal(CreateApiKeyResultCode.ApiKeysDisabledError, created.ResultCode);
        Assert.Null(created.Key);
    }

    [Fact]
    public async Task ByDefault_AKeyCannotSignIn()
    {
        var signIn = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .SignInWithApiKeyAsync(
                    new SignInWithApiKeyRequest("ciam_abcdefgh_secret", "device")
                )
        );

        Assert.Equal(SignInResultCode.InvalidApiKeyError, signIn.ResultCode);
    }
}
