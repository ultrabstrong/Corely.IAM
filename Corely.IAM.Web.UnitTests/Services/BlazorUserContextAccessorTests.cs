using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.UnitTests.Services;

public class BlazorUserContextAccessorTests
{
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor = new();
    private readonly BlazorUserContextAccessor _accessor;

    public BlazorUserContextAccessorTests()
    {
        _accessor = new BlazorUserContextAccessor(
            _mockUserContextProvider.Object,
            _mockHttpContextAccessor.Object
        );
    }

    private static UserContext CreateTestUserContext()
    {
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };
        return PageTestHelpers.CreateUserContext(
            currentAccount: account,
            availableAccounts: [account]
        );
    }

    private void SetupHttpContextWithCookie(string cookieValue)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] =
            $"{AuthenticationConstants.AUTH_TOKEN_COOKIE}={cookieValue}";
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
    }

    [Fact]
    public async Task GetUserContextAsync_ProviderAlreadyHasContext_ReturnsExistingContext()
    {
        var expectedContext = CreateTestUserContext();
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns(expectedContext);

        var result = await _accessor.GetUserContextAsync();

        Assert.Same(expectedContext, result);
        _mockUserContextProvider.Verify(
            p => p.SetUserContextAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetUserContextAsync_NoExistingContext_NoHttpContext_ReturnsNull()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var result = await _accessor.GetUserContextAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserContextAsync_NoExistingContext_NoAuthCookie_ReturnsNull()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var result = await _accessor.GetUserContextAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserContextAsync_ValidCookie_SetUserContextSucceeds_ReturnsContext()
    {
        var expectedContext = CreateTestUserContext();
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        SetupHttpContextWithCookie("valid-token");

        _mockUserContextProvider
            .Setup(p => p.SetUserContextAsync("valid-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.Success)
            .Callback(() =>
            {
                _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns(expectedContext);
            });

        var result = await _accessor.GetUserContextAsync();

        Assert.Same(expectedContext, result);
        _mockUserContextProvider.Verify(p => p.SetUserContextAsync("valid-token"), Times.Once);
    }

    [Fact]
    public async Task GetUserContextAsync_ValidCookie_SetUserContextFails_ReturnsNull()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        SetupHttpContextWithCookie("bad-token");

        _mockUserContextProvider
            .Setup(p => p.SetUserContextAsync("bad-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.TokenValidationFailed);

        var result = await _accessor.GetUserContextAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserContextAsync_AfterFailedValidation_RetriesOnNextCall()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        SetupHttpContextWithCookie("bad-token");

        _mockUserContextProvider
            .Setup(p => p.SetUserContextAsync("bad-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.TokenValidationFailed);

        var firstResult = await _accessor.GetUserContextAsync();
        var secondResult = await _accessor.GetUserContextAsync();

        Assert.Null(firstResult);
        Assert.Null(secondResult);
        _mockUserContextProvider.Verify(
            p => p.SetUserContextAsync(It.IsAny<string>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task GetUserContextAsync_SetUserContextThrows_ReturnsNull()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        SetupHttpContextWithCookie("error-token");

        _mockUserContextProvider
            .Setup(p => p.SetUserContextAsync("error-token"))
            .ThrowsAsync(new InvalidOperationException("Something broke"));

        var result = await _accessor.GetUserContextAsync();

        Assert.Null(result);
    }
}
