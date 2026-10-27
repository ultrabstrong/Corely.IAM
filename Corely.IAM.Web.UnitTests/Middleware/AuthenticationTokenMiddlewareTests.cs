using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Middleware;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.UnitTests.Middleware;

public class AuthenticationTokenMiddlewareTests
{
    private readonly Mock<IAuthenticationService> _mockAuthenticationService;
    private readonly Mock<IUserContextProvider> _mockUserContextProvider;
    private readonly Mock<ILogger<AuthenticationTokenMiddleware>> _mockLogger;
    private readonly IAuthCookieManager _authCookieManager = new AuthCookieManager(
        TimeProvider.System
    );
    private readonly IUserContextClaimsBuilder _userContextClaimsBuilder =
        new UserContextClaimsBuilder();

    public AuthenticationTokenMiddlewareTests()
    {
        _mockAuthenticationService = new Mock<IAuthenticationService>();
        _mockUserContextProvider = new Mock<IUserContextProvider>();
        _mockLogger = new Mock<ILogger<AuthenticationTokenMiddleware>>();
    }

    private AuthenticationTokenMiddleware CreateMiddleware(
        RequestDelegate next,
        bool acceptBearerTokens = false
    )
    {
        return new AuthenticationTokenMiddleware(
            next,
            _mockLogger.Object,
            _authCookieManager,
            _userContextClaimsBuilder,
            Options.Create(new IAMWebOptions { AcceptBearerTokens = acceptBearerTokens })
        );
    }

    [Fact]
    public async Task Invoke_WithBearerHeader_IgnoresTheCookies_WhenBearerTokensAreAccepted()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{AuthCookieManager.AUTH_TOKEN_COOKIE}=cookie-token";
        context.Request.Headers.Authorization = "Bearer header-token";
        var middleware = CreateMiddleware(_ => Task.CompletedTask, acceptBearerTokens: true);

        await middleware.InvokeAsync(
            context,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        _mockAuthenticationService.Verify(
            s => s.AuthenticateWithTokenAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Invoke_WithBearerHeader_ReadsTheCookies_WhenBearerTokensAreOff()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{AuthCookieManager.AUTH_TOKEN_COOKIE}=cookie-token";
        context.Request.Headers.Authorization = "Bearer header-token";
        _mockAuthenticationService
            .Setup(s => s.AuthenticateWithTokenAsync("cookie-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.Success);
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(
            context,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        _mockAuthenticationService.Verify(
            s => s.AuthenticateWithTokenAsync("cookie-token"),
            Times.Once
        );
    }

    [Fact]
    public async Task Invoke_NoCookie_CallsNextWithoutSettingUser()
    {
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(
            httpContext,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        Assert.True(nextCalled);
        Assert.False(httpContext.User.Identity?.IsAuthenticated ?? false);
        _mockAuthenticationService.Verify(
            x => x.AuthenticateWithTokenAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Invoke_ValidCookieAndSuccessfulValidation_SetsUserPrincipal()
    {
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = $"{AuthCookieManager.AUTH_TOKEN_COOKIE}=test-token";

        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };
        var userContext = PageTestHelpers.CreateUserContext(
            currentAccount: account,
            availableAccounts: [account]
        );
        _mockAuthenticationService
            .Setup(x => x.AuthenticateWithTokenAsync("test-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.Success);
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(userContext);

        await middleware.InvokeAsync(
            httpContext,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        Assert.True(nextCalled);
        Assert.True(httpContext.User.Identity?.IsAuthenticated);
        Assert.Equal(
            userContext.User!.Id.ToString(),
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
    public async Task Invoke_ValidationFails_DeletesCookieAndCallsNext(
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
            $"{AuthCookieManager.AUTH_TOKEN_COOKIE}=bad-token; {AuthCookieManager.REFRESH_TOKEN_COOKIE}=bad-refresh";

        _mockAuthenticationService
            .Setup(x => x.AuthenticateWithTokenAsync("bad-token"))
            .ReturnsAsync(resultCode);
        _mockAuthenticationService
            .Setup(x =>
                x.RenewAuthTokenAsync(
                    It.Is<RenewAuthTokenRequest>(r => r.RefreshToken == "bad-refresh")
                )
            )
            .ReturnsAsync(
                new RenewAuthTokenResult(
                    RenewAuthTokenResultCode.InvalidAuthTokenError,
                    "invalid",
                    null,
                    null,
                    null,
                    null
                )
            );

        await middleware.InvokeAsync(
            httpContext,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        Assert.True(nextCalled);
        Assert.False(httpContext.User.Identity?.IsAuthenticated ?? false);
        Assert.Contains(
            httpContext.Response.Headers.SetCookie,
            c => c != null && c.Contains(AuthCookieManager.AUTH_TOKEN_COOKIE)
        );
        Assert.Contains(
            httpContext.Response.Headers.SetCookie,
            c => c != null && c.Contains(AuthCookieManager.DEVICE_ID_COOKIE)
        );
    }

    [Fact]
    public async Task Invoke_ValidationFailsButRenewSucceeds_SetsUpdatedCookiesAndPrincipal()
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
            $"{AuthCookieManager.AUTH_TOKEN_COOKIE}=expired-token; {AuthCookieManager.REFRESH_TOKEN_COOKIE}=refresh-token";

        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };
        var userContext = PageTestHelpers.CreateUserContext(
            currentAccount: account,
            availableAccounts: [account]
        );
        _mockAuthenticationService
            .Setup(x => x.AuthenticateWithTokenAsync("expired-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.TokenValidationFailed);
        _mockAuthenticationService
            .Setup(x =>
                x.RenewAuthTokenAsync(
                    It.Is<RenewAuthTokenRequest>(r => r.RefreshToken == "refresh-token")
                )
            )
            .ReturnsAsync(
                new RenewAuthTokenResult(
                    RenewAuthTokenResultCode.Success,
                    null,
                    "renewed-token",
                    Guid.CreateVersion7(),
                    "renewed-refresh",
                    DateTime.UtcNow.AddDays(1)
                )
            );
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(userContext);

        await middleware.InvokeAsync(
            httpContext,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        Assert.True(nextCalled);
        Assert.True(httpContext.User.Identity?.IsAuthenticated);
        Assert.Contains(
            httpContext.Response.Headers.SetCookie,
            c => c != null && c.Contains($"{AuthCookieManager.AUTH_TOKEN_COOKIE}=renewed-token")
        );
        Assert.Contains(
            httpContext.Response.Headers.SetCookie,
            c =>
                c != null && c.Contains($"{AuthCookieManager.REFRESH_TOKEN_COOKIE}=renewed-refresh")
        );
    }

    [Fact]
    public async Task Invoke_OnlyRefreshCookie_RenewsWithoutValidatingAnAccessToken()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] =
            $"{AuthCookieManager.REFRESH_TOKEN_COOKIE}=refresh-token";
        var userContext = PageTestHelpers.CreateUserContext();
        _mockAuthenticationService
            .Setup(x =>
                x.RenewAuthTokenAsync(
                    It.Is<RenewAuthTokenRequest>(r => r.RefreshToken == "refresh-token")
                )
            )
            .ReturnsAsync(
                new RenewAuthTokenResult(
                    RenewAuthTokenResultCode.Success,
                    null,
                    "renewed-token",
                    Guid.CreateVersion7(),
                    "renewed-refresh",
                    DateTime.UtcNow.AddDays(1)
                )
            );
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(userContext);

        await middleware.InvokeAsync(
            httpContext,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        Assert.True(httpContext.User.Identity?.IsAuthenticated);
        _mockAuthenticationService.Verify(
            x => x.AuthenticateWithTokenAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Invoke_RefreshTokenAlreadyRotated_KeepsCookies()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] =
            $"{AuthCookieManager.AUTH_TOKEN_COOKIE}=expired-token; {AuthCookieManager.REFRESH_TOKEN_COOKIE}=refresh-token";
        _mockAuthenticationService
            .Setup(x => x.AuthenticateWithTokenAsync("expired-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.TokenValidationFailed);
        _mockAuthenticationService
            .Setup(x => x.RenewAuthTokenAsync(It.IsAny<RenewAuthTokenRequest>()))
            .ReturnsAsync(
                new RenewAuthTokenResult(
                    RenewAuthTokenResultCode.RefreshTokenAlreadyRotatedError,
                    "rotated",
                    null,
                    null,
                    null,
                    null
                )
            );

        await middleware.InvokeAsync(
            httpContext,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        Assert.False(httpContext.User.Identity?.IsAuthenticated ?? false);
        Assert.Equal(0, httpContext.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public async Task Invoke_InvalidAccessTokenAndNoRefreshCookie_DeletesCookiesWithoutRenewing()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = $"{AuthCookieManager.AUTH_TOKEN_COOKIE}=bad-token";
        _mockAuthenticationService
            .Setup(x => x.AuthenticateWithTokenAsync("bad-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.TokenValidationFailed);

        await middleware.InvokeAsync(
            httpContext,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        Assert.Contains(
            httpContext.Response.Headers.SetCookie,
            c => c != null && c.Contains(AuthCookieManager.AUTH_TOKEN_COOKIE)
        );
        _mockAuthenticationService.Verify(
            x => x.RenewAuthTokenAsync(It.IsAny<RenewAuthTokenRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Invoke_ExceptionDuringValidation_DeletesCookieAndCallsNext()
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
            $"{AuthCookieManager.AUTH_TOKEN_COOKIE}=error-token";

        _mockAuthenticationService
            .Setup(x => x.AuthenticateWithTokenAsync("error-token"))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        await middleware.InvokeAsync(
            httpContext,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        Assert.True(nextCalled);
        Assert.False(httpContext.User.Identity?.IsAuthenticated ?? false);
        Assert.Contains(
            httpContext.Response.Headers.SetCookie,
            c => c != null && c.Contains(AuthCookieManager.AUTH_TOKEN_COOKIE)
        );
        Assert.Contains(
            httpContext.Response.Headers.SetCookie,
            c => c != null && c.Contains(AuthCookieManager.DEVICE_ID_COOKIE)
        );
    }

    [Fact]
    public async Task Invoke_SuccessButNullUserContext_DoesNotSetPrincipal()
    {
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = $"{AuthCookieManager.AUTH_TOKEN_COOKIE}=test-token";

        _mockAuthenticationService
            .Setup(x => x.AuthenticateWithTokenAsync("test-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.Success);
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns((UserContext?)null);

        await middleware.InvokeAsync(
            httpContext,
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object
        );

        Assert.True(nextCalled);
        Assert.False(httpContext.User.Identity?.IsAuthenticated ?? false);
    }
}
