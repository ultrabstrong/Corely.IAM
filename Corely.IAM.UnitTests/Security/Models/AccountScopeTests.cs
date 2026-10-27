using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Security.Models;

public class AccountScopeTests
{
    private static readonly Guid _accountId = Guid.CreateVersion7();
    private static readonly Guid _otherAccountId = Guid.CreateVersion7();
    private static readonly Guid _rowId = Guid.CreateVersion7();

    private static GroupEntity Row(Guid accountId, Guid? id = null) =>
        new() { Id = id ?? _rowId, AccountId = accountId };

    private static bool Matches(AccountScope scope, GroupEntity row) =>
        scope.ByIdPredicate<GroupEntity>(_rowId).Compile()(row);

    [Fact]
    public void Of_MatchesOnlyTheRowInThatAccount()
    {
        var scope = AccountScope.Of(_accountId);

        Assert.True(Matches(scope, Row(_accountId)));
        Assert.False(Matches(scope, Row(_otherAccountId)));
        Assert.False(Matches(scope, Row(_accountId, Guid.CreateVersion7())));
    }

    [Fact]
    public void Any_MatchesTheRowInEveryAccount()
    {
        Assert.True(Matches(AccountScope.Any, Row(_accountId)));
        Assert.True(Matches(AccountScope.Any, Row(_otherAccountId)));
    }

    [Fact]
    public void ADefaultOrEmptyScope_MatchesNothing()
    {
        Assert.False(Matches(default, Row(Guid.Empty)));
        Assert.False(Matches(AccountScope.Of(Guid.Empty), Row(Guid.Empty)));
        Assert.False(default(AccountScope).Includes(Guid.Empty));
    }

    [Fact]
    public void AGuid_ConvertsToAScopeOfThatAccount()
    {
        AccountScope scope = _accountId;

        Assert.Equal(AccountScope.Of(_accountId), scope);
        Assert.False(scope.IsAny);
        Assert.True(scope.Includes(_accountId));
        Assert.False(scope.Includes(_otherAccountId));
    }

    [Fact]
    public void CurrentAccountScope_IsAny_OnlyForSystemContextWithoutAnAccount()
    {
        var account = new Account { Id = _accountId, AccountName = "Acme" };

        Assert.Equal(AccountScope.Any, new UserContext(true, "device").CurrentAccountScope);
        Assert.Equal(
            AccountScope.Of(_accountId),
            (new UserContext(true, "device") with { CurrentAccount = account }).CurrentAccountScope
        );
        Assert.Equal(
            AccountScope.Of(_accountId),
            new UserContext(
                new User { Id = Guid.CreateVersion7(), Username = "u" },
                account,
                "device",
                [account]
            ).CurrentAccountScope
        );
        Assert.Equal(
            AccountScope.Of(Guid.Empty),
            new UserContext(
                new User { Id = Guid.CreateVersion7(), Username = "u" },
                null,
                "device",
                []
            ).CurrentAccountScope
        );
    }
}
