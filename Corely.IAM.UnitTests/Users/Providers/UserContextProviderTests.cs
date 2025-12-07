using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Users.Providers;

public class UserContextProviderTests
{
    private readonly UserContextProvider _provider = new();

    [Fact]
    public void GetUserContext_ReturnsNull_WhenNotSet()
    {
        var result = _provider.GetUserContext();

        Assert.Null(result);
    }

    [Fact]
    public void GetUserContext_ReturnsContext_WhenSet()
    {
        var context = new UserContext(1, 2);
        _provider.SetUserContext(context);

        var result = _provider.GetUserContext();

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(2, result.AccountId);
    }

    [Fact]
    public void SetUserContext_OverwritesPreviousContext()
    {
        var context1 = new UserContext(1, 2);
        var context2 = new UserContext(3, 4);

        _provider.SetUserContext(context1);
        _provider.SetUserContext(context2);

        var result = _provider.GetUserContext();

        Assert.NotNull(result);
        Assert.Equal(3, result.UserId);
        Assert.Equal(4, result.AccountId);
    }
}
