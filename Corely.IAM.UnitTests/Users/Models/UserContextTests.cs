using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Users.Models;

public class UserContextTests
{
    private static readonly Account _member = new()
    {
        Id = Guid.CreateVersion7(),
        AccountName = "Member",
    };
    private static readonly Account _entered = new()
    {
        Id = Guid.CreateVersion7(),
        AccountName = "Entered",
    };
    private static readonly User _user = new() { Id = Guid.CreateVersion7(), Username = "user" };

    [Fact]
    public void MemberAccounts_LeavesOutTheCurrentAccount_WhenEnteredAsPlatformMember()
    {
        var context = new UserContext(_user, _entered, "device", [_member, _entered], null, true);

        Assert.Equal([_member], context.MemberAccounts);
    }

    [Fact]
    public void MemberAccounts_KeepsTheCurrentAccount_WhenItIsAMembership()
    {
        var context = new UserContext(_user, _member, "device", [_member, _entered]);

        Assert.Equal([_member, _entered], context.MemberAccounts);
    }
}
