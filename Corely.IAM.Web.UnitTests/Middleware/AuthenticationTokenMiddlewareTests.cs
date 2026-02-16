using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Middleware;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Web.UnitTests.Middleware;

public class AuthenticationTokenMiddlewareTests
{
    private readonly Mock<IUserContextProvider> _mockUserContextProvider;
    private readonly Mock<ILogger<AuthenticationTokenMiddleware>> _mockLogger;
    private readonly IAuthCookieManager _authCookieManager = new AuthCookieManager();
    private readonly IUserContextClaimsBuilder _userContextClaimsBuilder =
        new UserContextClaimsBuilder();

    public AuthenticationTokenMiddlewareTests()
    {
        _mockUserContextProvider = new Mock<IUserContextProvider>();
        _mockLogger = new Mock<ILogger<AuthenticationTokenMiddleware>>();
    }

    private AuthenticationTokenMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new AuthenticationTokenMiddleware(
            next,
            _mockLogger.Object,
            _authCookieManager,
            _userContextClaimsBuilder
        );
    }

    [Fact]
    public async Task InvokeAsync_NoCookie_CallsNextWithoutSettingUser()
    {
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext, _mockUserContextProvider.Object);

        Assert.True(nextCalled);
        Assert.False(httpContext.User.Identity?.IsAuthenticated ?? false);
        _mockUserContextProvider.Verify(
            x => x.SetUserContextAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task InvokeAsync_ValidCookieAndSuccessfulValidation_SetsUserPrincipal()
    {
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] =
            $"{AuthenticationConstants.AUTH_TOKEN_COOKIE}=test-token";

        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };
        var userContext = PageTestHelpers.CreateUserContext(
            currentAccount: account,
            availableAccounts: [account]
        );
        _mockUserContextProvider
            .Setup(x => x.SetUserContextAsync("test-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.Success);
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(userContext);

        await middleware.InvokeAsync(httpContext, _mockUserContextProvider.Object);

        Assert.True(nextCalled);
        Assert.True(httpContext.User.Identity?.IsAuthenticated);
        Assert.Equal(
            userContext.User.Id.ToString(),
            httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        );
        Assert.Equal(
            userContext.User.Username,
            httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        );
    }

    [Theory]
    [InlineData(UserAuthTokenValidationResultCode.TokenValidationFailed)]
    [InlineData(UserAuthTokenValidationResultCode.InvalidTokenFormat)]
    [InlineData(UserAuthTokenValidationResultCode.MissingUserIdClaim)]
    [InlineData(UserAuthTokenValidationResultCode.MissingDeviceIdClaim)]
    public async Task InvokeAsync_ValidationFails_DeletesCookieAndCallsNext(
        UserAuthTokenValidationResultCode resultCode
    )
    {
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] =
            $"{AuthenticationConstants.AUTH_TOKEN_COOKIE}=bad-token";

        _mockUserContextProvider
            .Setup(x => x.SetUserContextAsync("bad-token"))
            .ReturnsAsync(resultCode);

        await middleware.InvokeAsync(httpContext, _mockUserContextProvider.Object);

        Assert.True(nextCalled);
        Assert.False(httpContext.User.Identity?.IsAuthenticated ?? false);
        Assert.Contains(
            httpContext.Response.Headers.SetCookie,
            c => c != null && c.Contains(AuthenticationConstants.AUTH_TOKEN_COOKIE)
        );
    }

    [Fact]
    public async Task InvokeAsync_ExceptionDuringValidation_DeletesCookieAndCallsNext()
    {
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] =
            $"{AuthenticationConstants.AUTH_TOKEN_COOKIE}=error-token";

        _mockUserContextProvider
            .Setup(x => x.SetUserContextAsync("error-token"))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        await middleware.InvokeAsync(httpContext, _mockUserContextProvider.Object);

        Assert.True(nextCalled);
        Assert.False(httpContext.User.Identity?.IsAuthenticated ?? false);
        Assert.Contains(
            httpContext.Response.Headers.SetCookie,
            c => c != null && c.Contains(AuthenticationConstants.AUTH_TOKEN_COOKIE)
        );
    }

    [Fact]
    public async Task InvokeAsync_SuccessButNullUserContext_DoesNotSetPrincipal()
    {
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] =
            $"{AuthenticationConstants.AUTH_TOKEN_COOKIE}=test-token";

        _mockUserContextProvider
            .Setup(x => x.SetUserContextAsync("test-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.Success);
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns((UserContext?)null);

        await middleware.InvokeAsync(httpContext, _mockUserContextProvider.Object);

        Assert.True(nextCalled);
        Assert.False(httpContext.User.Identity?.IsAuthenticated ?? false);
    }
}
