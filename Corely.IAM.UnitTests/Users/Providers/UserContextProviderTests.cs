using AutoFixture;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Users.Providers;

public class UserContextProviderTests
{
    private const string TEST_DEVICE_ID = "test-device";

    private readonly Fixture _fixture = new();
    private readonly Mock<IAuthenticationProvider> _mockAuthenticationProvider = new();
    private readonly UserContextProvider _provider;

    public UserContextProviderTests()
    {
        _fixture.Customize<Account>(c =>
            c.Without(a => a.SymmetricKeys).Without(a => a.AsymmetricKeys)
        );
        _provider = new UserContextProvider(_mockAuthenticationProvider.Object);
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
            new User() { Id = 1 },
            new Account() { Id = 2 },
            TEST_DEVICE_ID,
            [_fixture.Create<Account>()]
        );
        ((IUserContextSetter)_provider).SetUserContext(context);

        var result = _provider.GetUserContext();

        Assert.NotNull(result);
        Assert.Equal(1, result.User.Id);
        Assert.NotNull(result.CurrentAccount);
        Assert.Equal(2, result.CurrentAccount.Id);
        Assert.Equal(TEST_DEVICE_ID, result.DeviceId);
        Assert.Single(result.AvailableAccounts);
    }

    [Fact]
    public void SetUserContext_OverwritesPreviousContext()
    {
        var context1 = new UserContext(
            new User() { Id = 1 },
            new Account() { Id = 2 },
            TEST_DEVICE_ID,
            [_fixture.Create<Account>()]
        );
        var context2 = new UserContext(
            new User() { Id = 3 },
            new Account() { Id = 4 },
            "other-device",
            [_fixture.Create<Account>()]
        );

        var setter = _provider;
        setter.SetUserContext(context1);
        setter.SetUserContext(context2);

        var result = _provider.GetUserContext();

        Assert.NotNull(result);
        Assert.Equal(3, result.User.Id);
        Assert.NotNull(result.CurrentAccount);
        Assert.Equal(4, result.CurrentAccount.Id);
        Assert.Equal("other-device", result.DeviceId);
        Assert.Single(result.AvailableAccounts);
        Assert.Equal(context2.AvailableAccounts, result.AvailableAccounts);
    }

    [Fact]
    public async Task SetUserContextAsync_ReturnsResultCode_WhenTokenValidationFails()
    {
        var token = "some-token";
        _mockAuthenticationProvider
            .Setup(p => p.ValidateUserAuthTokenAsync(token))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.TokenValidationFailed,
                    null,
                    null,
                    null,
                    []
                )
            );

        var result = await _provider.SetUserContextAsync(token);

        Assert.Equal(UserAuthTokenValidationResultCode.TokenValidationFailed, result);
        Assert.Null(_provider.GetUserContext());
    }

    [Fact]
    public async Task SetUserContextAsync_ReturnsSuccess_WhenTokenIsValid()
    {
        var token = "valid-token";
        var account = _fixture.Create<Account>();
        _mockAuthenticationProvider
            .Setup(p => p.ValidateUserAuthTokenAsync(token))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.Success,
                    new User() { Id = 42 },
                    new Account() { Id = 100 },
                    TEST_DEVICE_ID,
                    [account]
                )
            );

        var result = await _provider.SetUserContextAsync(token);

        Assert.Equal(UserAuthTokenValidationResultCode.Success, result);
        var context = _provider.GetUserContext();
        Assert.NotNull(context);
        Assert.Equal(42, context.User.Id);
        Assert.NotNull(context.CurrentAccount);
        Assert.Equal(100, context.CurrentAccount.Id);
        Assert.Equal(TEST_DEVICE_ID, context.DeviceId);
        Assert.Single(context.AvailableAccounts);
        Assert.Equal(account, context.AvailableAccounts[0]);
    }

    [Fact]
    public async Task SetUserContextAsync_SetsContextWithNullAccountId_WhenNoSignedInAccountClaim()
    {
        var token = "valid-token";
        _mockAuthenticationProvider
            .Setup(p => p.ValidateUserAuthTokenAsync(token))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.Success,
                    new User() { Id = 42 },
                    null,
                    TEST_DEVICE_ID,
                    []
                )
            );

        var result = await _provider.SetUserContextAsync(token);

        Assert.Equal(UserAuthTokenValidationResultCode.Success, result);
        var context = _provider.GetUserContext();
        Assert.NotNull(context);
        Assert.Equal(42, context.User.Id);
        Assert.Null(context.CurrentAccount);
        Assert.Equal(TEST_DEVICE_ID, context.DeviceId);
        Assert.Empty(context.AvailableAccounts);
    }

    [Theory]
    [InlineData(UserAuthTokenValidationResultCode.InvalidTokenFormat)]
    [InlineData(UserAuthTokenValidationResultCode.MissingUserIdClaim)]
    [InlineData(UserAuthTokenValidationResultCode.TokenValidationFailed)]
    public async Task SetUserContextAsync_ReturnsCorrectResultCode_ForEachFailureType(
        UserAuthTokenValidationResultCode expectedResultCode
    )
    {
        var token = "some-token";
        _mockAuthenticationProvider
            .Setup(p => p.ValidateUserAuthTokenAsync(token))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(expectedResultCode, null, null, null, [])
            );

        var result = await _provider.SetUserContextAsync(token);

        Assert.Equal(expectedResultCode, result);
        Assert.Null(_provider.GetUserContext());
    }

    [Fact]
    public async Task SetUserContextAsync_ReturnsMissingDeviceIdClaim_WhenDeviceIdIsNull()
    {
        var token = "valid-token";
        _mockAuthenticationProvider
            .Setup(p => p.ValidateUserAuthTokenAsync(token))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.Success,
                    new User() { Id = 42 },
                    new Account() { Id = 100 },
                    null, // DeviceId is null
                    []
                )
            );

        var result = await _provider.SetUserContextAsync(token);

        Assert.Equal(UserAuthTokenValidationResultCode.MissingDeviceIdClaim, result);
        Assert.Null(_provider.GetUserContext());
    }

    [Fact]
    public async Task SetUserContextAsync_ReturnsMissingDeviceIdClaim_WhenDeviceIdIsEmpty()
    {
        var token = "valid-token";
        _mockAuthenticationProvider
            .Setup(p => p.ValidateUserAuthTokenAsync(token))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.Success,
                    new User() { Id = 42 },
                    new Account() { Id = 100 },
                    "", // DeviceId is empty
                    []
                )
            );

        var result = await _provider.SetUserContextAsync(token);

        Assert.Equal(UserAuthTokenValidationResultCode.MissingDeviceIdClaim, result);
        Assert.Null(_provider.GetUserContext());
    }

    [Fact]
    public void ClearUserContext_RemovesContext_WhenUserIdMatches()
    {
        var context = new UserContext(
            new User() { Id = 1 },
            new Account() { Id = 2 },
            TEST_DEVICE_ID,
            []
        );
        _provider.SetUserContext(context);

        _provider.ClearUserContext(1);

        Assert.Null(_provider.GetUserContext());
    }

    [Fact]
    public void ClearUserContext_DoesNotRemoveContext_WhenUserIdDoesNotMatch()
    {
        var context = new UserContext(
            new User() { Id = 1 },
            new Account() { Id = 2 },
            TEST_DEVICE_ID,
            [_fixture.Create<Account>()]
        );
        _provider.SetUserContext(context);

        _provider.ClearUserContext(2);

        var result = _provider.GetUserContext();
        Assert.NotNull(result);
        Assert.Equal(1, result.User.Id);
        Assert.NotNull(result.CurrentAccount);
        Assert.Equal(2, result.CurrentAccount.Id);
        Assert.Equal(TEST_DEVICE_ID, result.DeviceId);
        Assert.Single(result.AvailableAccounts);
    }
}
