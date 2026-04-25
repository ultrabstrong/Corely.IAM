using AutoFixture;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Users.Providers;

public class UserContextProviderTests
{
    private const string TEST_DEVICE_ID = "test-device";

    private readonly Fixture _fixture = new();
    private readonly UserContextProvider _provider;

    public UserContextProviderTests()
    {
        _fixture.Customize<Account>(c =>
            c.Without(a => a.SymmetricKeys).Without(a => a.AsymmetricKeys)
        );
        _provider = new UserContextProvider();
    }

    [Fact]
    public void GetUserContext_ReturnsNull_WhenNotSet()
    {
        var result = _provider.GetUserContext();

        Assert.Null(result);
    }

    [Fact]
    public void GetUserContext_ReturnsContext_WhenSetViaInternalSetter()
    {
        var context = new UserContext(
            new User() { Id = Guid.CreateVersion7() },
            new Account() { Id = Guid.CreateVersion7() },
            TEST_DEVICE_ID,
            [_fixture.Create<Account>()]
        );
        ((IUserContextSetter)_provider).SetUserContext(context);

        var result = _provider.GetUserContext();

        Assert.NotNull(result);
        Assert.Equal(context.User.Id, result.User.Id);
        Assert.NotNull(result.CurrentAccount);
        Assert.Equal(context.CurrentAccount!.Id, result.CurrentAccount.Id);
        Assert.Equal(TEST_DEVICE_ID, result.DeviceId);
        Assert.Single(result.AvailableAccounts);
    }

    [Fact]
    public void SetUserContext_OverwritesPreviousContext()
    {
        var context1 = new UserContext(
            new User() { Id = Guid.CreateVersion7() },
            new Account() { Id = Guid.CreateVersion7() },
            TEST_DEVICE_ID,
            [_fixture.Create<Account>()]
        );
        var context2 = new UserContext(
            new User() { Id = Guid.CreateVersion7() },
            new Account() { Id = Guid.CreateVersion7() },
            "other-device",
            [_fixture.Create<Account>()]
        );

        var setter = _provider;
        setter.SetUserContext(context1);
        setter.SetUserContext(context2);

        var result = _provider.GetUserContext();

        Assert.NotNull(result);
        Assert.Equal(context2.User.Id, result.User.Id);
        Assert.NotNull(result.CurrentAccount);
        Assert.Equal(context2.CurrentAccount!.Id, result.CurrentAccount.Id);
        Assert.Equal("other-device", result.DeviceId);
        Assert.Single(result.AvailableAccounts);
        Assert.Equal(context2.AvailableAccounts, result.AvailableAccounts);
    }

    [Fact]
    public void SetSystemContext_SetsSystemContext()
    {
        ((IUserContextSetter)_provider).SetSystemContext(TEST_DEVICE_ID);

        var result = _provider.GetUserContext();

        Assert.NotNull(result);
        Assert.True(result.IsSystemContext);
        Assert.Equal(TEST_DEVICE_ID, result.DeviceId);
    }

    [Fact]
    public void ClearUserContext_RemovesContext_WhenUserIdMatches()
    {
        var context = new UserContext(
            new User() { Id = Guid.CreateVersion7() },
            new Account() { Id = Guid.CreateVersion7() },
            TEST_DEVICE_ID,
            []
        );
        _provider.SetUserContext(context);

        _provider.ClearUserContext(context.User.Id);

        Assert.Null(_provider.GetUserContext());
    }

    [Fact]
    public void ClearUserContext_DoesNotRemoveContext_WhenUserIdDoesNotMatch()
    {
        var context = new UserContext(
            new User() { Id = Guid.CreateVersion7() },
            new Account() { Id = Guid.CreateVersion7() },
            TEST_DEVICE_ID,
            [_fixture.Create<Account>()]
        );
        _provider.SetUserContext(context);

        _provider.ClearUserContext(Guid.CreateVersion7());

        var result = _provider.GetUserContext();
        Assert.NotNull(result);
        Assert.Equal(context.User.Id, result.User.Id);
        Assert.NotNull(result.CurrentAccount);
        Assert.Equal(context.CurrentAccount!.Id, result.CurrentAccount.Id);
        Assert.Equal(TEST_DEVICE_ID, result.DeviceId);
        Assert.Single(result.AvailableAccounts);
    }
}
