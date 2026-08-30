using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

/// <summary>
/// I5 - nullable comparisons behave differently in SQL than in memory, and the difference is
/// silent. In C# <c>null != x</c> is true; in SQL a comparison involving NULL is UNKNOWN, so rows
/// with NULL are excluded from both <c>= x</c> and <c>&lt;&gt; x</c>. An in-memory test double
/// cannot reproduce that.
///
/// <c>UserAuthTokenEntity.AccountId</c> is the live example: it is null until an account is
/// selected.
/// </summary>
public class NullablePredicateTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public Task InitializeAsync() => _scenario.InitializeAsync();

    public Task DisposeAsync() => _scenario.DisposeAsync();

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

    /// <summary>
    /// Pins down behaviour that is easy to get wrong in both directions.
    ///
    /// In raw SQL, <c>AccountId &lt;&gt; @value</c> excludes NULL rows, because a comparison
    /// involving NULL is UNKNOWN rather than true. EF Core deliberately compensates: it emits
    /// <c>AccountId &lt;&gt; @value OR AccountId IS NULL</c> so the result matches C# semantics,
    /// where <c>null != value</c> is true.
    ///
    /// The consequence worth knowing: a LINQ inequality and the "same" hand-written SQL return
    /// different rows. Anyone dropping to raw SQL for one of these predicates has to add the
    /// null branch back by hand.
    /// </summary>
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
