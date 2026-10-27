using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public class DirectAccountMembershipTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    private static Task<RegisterUserWithAccountResult> AddAsync(
        IServiceProvider services,
        Guid userId,
        Guid accountId
    ) =>
        services
            .GetRequiredService<IRegistrationService>()
            .RegisterUserWithAccountAsync(new RegisterUserWithAccountRequest(userId, accountId));

    [Fact]
    public async Task AnOwner_CannotAddAUserToTheirAccountDirectly()
    {
        var added = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services => AddAsync(services, _scenario.OutsiderUserId, _scenario.AccountId)
        );

        Assert.Equal(RegisterUserWithAccountResultCode.UnauthorizedError, added.ResultCode);
    }

    [Fact]
    public async Task AUser_CannotAddThemselvesToAnAccountDirectly()
    {
        var added = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            null,
            services => AddAsync(services, _scenario.OutsiderUserId, _scenario.AccountId)
        );

        Assert.Equal(RegisterUserWithAccountResultCode.UnauthorizedError, added.ResultCode);
    }

    [Fact]
    public async Task SystemContext_AddsAUserDirectly()
    {
        var added = await _scenario.AsSystemAsync(services =>
            AddAsync(services, _scenario.OutsiderUserId, _scenario.AccountId)
        );

        Assert.Equal(RegisterUserWithAccountResultCode.Success, added.ResultCode);
    }
}
