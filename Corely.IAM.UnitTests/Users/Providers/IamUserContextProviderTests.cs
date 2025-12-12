using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Users.Providers;

public class IamUserContextProviderTests
{
    private readonly Mock<IAuthenticationProvider> _mockAuthenticationProvider = new();
    private readonly IamUserContextProvider _provider;

    public IamUserContextProviderTests()
    {
        _provider = new IamUserContextProvider(_mockAuthenticationProvider.Object);
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
        var context = new IamUserContext(1, 2);
        ((IIamUserContextSetter)_provider).SetUserContext(context);

        var result = _provider.GetUserContext();

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(2, result.AccountId);
    }

    [Fact]
    public void SetUserContext_OverwritesPreviousContext()
    {
        var context1 = new IamUserContext(1, 2);
        var context2 = new IamUserContext(3, 4);

        var setter = _provider;
        setter.SetUserContext(context1);
        setter.SetUserContext(context2);

        var result = _provider.GetUserContext();

        Assert.NotNull(result);
        Assert.Equal(3, result.UserId);
        Assert.Equal(4, result.AccountId);
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
                    null
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
        _mockAuthenticationProvider
            .Setup(p => p.ValidateUserAuthTokenAsync(token))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.Success,
                    42,
                    100
                )
            );

        var result = await _provider.SetUserContextAsync(token);

        Assert.Equal(UserAuthTokenValidationResultCode.Success, result);
        var context = _provider.GetUserContext();
        Assert.NotNull(context);
        Assert.Equal(42, context.UserId);
        Assert.Equal(100, context.AccountId);
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
                    42,
                    null
                )
            );

        var result = await _provider.SetUserContextAsync(token);

        Assert.Equal(UserAuthTokenValidationResultCode.Success, result);
        var context = _provider.GetUserContext();
        Assert.NotNull(context);
        Assert.Equal(42, context.UserId);
        Assert.Null(context.AccountId);
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
            .ReturnsAsync(new UserAuthTokenValidationResult(expectedResultCode, null, null));

        var result = await _provider.SetUserContextAsync(token);

        Assert.Equal(expectedResultCode, result);
        Assert.Null(_provider.GetUserContext());
    }
}
