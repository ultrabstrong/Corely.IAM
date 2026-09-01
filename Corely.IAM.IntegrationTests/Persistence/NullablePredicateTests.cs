using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

public class NullablePredicateTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task SigningInWithoutAnAccount_LeavesAccountIdNull()
    {
        await SignInWithoutAccountAsync();

        var nullAccountTokens = await _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking()
                .CountAsync(t => t.UserId == _scenario.OutsiderUserId && t.AccountId == null)
        );

        Assert.True(nullAccountTokens > 0);
    }

    [Fact]
    public async Task EqualityAgainstANonNullValue_ExcludesNullRows()
    {
        await SignInWithoutAccountAsync();
        await SignInAndSelectAccountAsync();

        var matching = await _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking()
                .Where(t => t.AccountId == _scenario.AccountId)
                .ToListAsync()
        );

        Assert.All(matching, t => Assert.Equal(_scenario.AccountId, t.AccountId));
        Assert.DoesNotContain(matching, t => t.AccountId == null);
    }

    [Fact]
    public async Task InequalityAgainstANonNullValue_IncludesNullRows_BecauseEfCompensates()
    {
        await SignInWithoutAccountAsync();
        await SignInAndSelectAccountAsync();

        var otherAccountId = _scenario.OtherAccountId;
        var matching = await _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking().Where(t => t.AccountId != otherAccountId).ToListAsync()
        );

        Assert.Contains(matching, t => t.AccountId == null);
    }

    [Fact]
    public async Task ExplicitNullCheck_IsTheOnlyWayToReachNullRows()
    {
        await SignInWithoutAccountAsync();

        var nullRows = await _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking().Where(t => t.AccountId == null).ToListAsync()
        );

        Assert.NotEmpty(nullRows);
        Assert.All(nullRows, t => Assert.Null(t.AccountId));
    }

    [Fact]
    public async Task OptionalDateColumns_TranslateForBothNullAndNonNullPredicates()
    {
        await SignInAndSelectAccountAsync();

        var unrevoked = await _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking().CountAsync(t => t.RevokedUtc == null)
        );
        var revoked = await _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking().CountAsync(t => t.RevokedUtc != null)
        );
        var total = await _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking().CountAsync()
        );

        Assert.Equal(total, unrevoked + revoked);
    }

    [Fact]
    public async Task NullableComparisonCombinedWithABooleanOperator_Translates()
    {
        await SignInAndSelectAccountAsync();
        var now = _scenario.Host.TimeProvider.GetUtcNow().UtcDateTime;

        var exception = await Record.ExceptionAsync(() =>
            _scenario.Host.QueryAsync(db =>
                db.UserAuthTokens.AsNoTracking()
                    .Where(t =>
                        t.AccountId == _scenario.AccountId
                        && t.RevokedUtc == null
                        && t.ExpiresUtc > now
                    )
                    .ToListAsync()
            )
        );

        Assert.Null(exception);
    }

    private Task SignInWithoutAccountAsync() =>
        _scenario.Host.WithScopeAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(
                    new SignInRequest(
                        _scenario.OutsiderUsername,
                        IamScenario.Password,
                        "device-outsider"
                    )
                );
            Assert.Equal(SignInResultCode.Success, result.ResultCode);
        });

    private Task SignInAndSelectAccountAsync() =>
        _scenario.ActAsAsync(
            _scenario.DirectMemberUsername,
            _scenario.AccountId,
            _ => Task.FromResult(true)
        );
}
