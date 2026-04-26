using Corely.IAM.Accounts.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Web.UnitTests.Services;

public class BlazorUserContextAccessorTests
{
    private readonly Mock<IAuthenticationService> _mockAuthenticationService = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor = new();
    private readonly Mock<ILogger<BlazorUserContextAccessor>> _mockLogger = new();
    private readonly BlazorUserContextAccessor _accessor;

    public BlazorUserContextAccessorTests()
    {
        _accessor = new BlazorUserContextAccessor(
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object,
            _mockHttpContextAccessor.Object,
            _mockLogger.Object
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
    public async Task GetUserContext_ProviderAlreadyHasContext_ReturnsExistingContext()
    {
        var expectedContext = CreateTestUserContext();
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns(expectedContext);

        var result = await _accessor.GetUserContextAsync();

        Assert.Same(expectedContext, result);
        _mockAuthenticationService.Verify(
            s => s.AuthenticateWithTokenAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetUserContext_NoExistingContext_NoHttpContext_ReturnsNull()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var result = await _accessor.GetUserContextAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserContext_NoExistingContext_NoAuthCookie_ReturnsNull()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var result = await _accessor.GetUserContextAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserContext_ValidCookie_SetUserContextSucceeds_ReturnsContext()
    {
        var expectedContext = CreateTestUserContext();
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        SetupHttpContextWithCookie("valid-token");

        _mockAuthenticationService
            .Setup(s => s.AuthenticateWithTokenAsync("valid-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.Success)
            .Callback(() =>
            {
                _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns(expectedContext);
            });

        var result = await _accessor.GetUserContextAsync();

        Assert.Same(expectedContext, result);
        _mockAuthenticationService.Verify(
            s => s.AuthenticateWithTokenAsync("valid-token"),
            Times.Once
        );
    }

    [Fact]
    public async Task GetUserContext_ValidCookie_SetUserContextFails_ReturnsNull()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        SetupHttpContextWithCookie("bad-token");

        _mockAuthenticationService
            .Setup(s => s.AuthenticateWithTokenAsync("bad-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.TokenValidationFailed);

        var result = await _accessor.GetUserContextAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserContext_AfterFailedValidation_RetriesOnNextCall()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        SetupHttpContextWithCookie("bad-token");

        _mockAuthenticationService
            .Setup(s => s.AuthenticateWithTokenAsync("bad-token"))
            .ReturnsAsync(UserAuthTokenValidationResultCode.TokenValidationFailed);

        var firstResult = await _accessor.GetUserContextAsync();
        var secondResult = await _accessor.GetUserContextAsync();

        Assert.Null(firstResult);
        Assert.Null(secondResult);
        _mockAuthenticationService.Verify(
            s => s.AuthenticateWithTokenAsync(It.IsAny<string>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task GetUserContext_SetUserContextThrows_ReturnsNull()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);
        SetupHttpContextWithCookie("error-token");

        _mockAuthenticationService
            .Setup(s => s.AuthenticateWithTokenAsync("error-token"))
            .ThrowsAsync(new InvalidOperationException("Something broke"));

        var result = await _accessor.GetUserContextAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserContext_ConcurrentCalls_ShareSingleAuthenticationAttempt()
    {
        var expectedContext = CreateTestUserContext();
        var authStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var releaseAuth = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        UserContext? currentContext = null;
        var authCalls = 0;

        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns(() => currentContext);
        SetupHttpContextWithCookie("valid-token");

        _mockAuthenticationService
            .Setup(s => s.AuthenticateWithTokenAsync("valid-token"))
            .Returns(async () =>
            {
                Interlocked.Increment(ref authCalls);
                authStarted.TrySetResult();
                await releaseAuth.Task;
                currentContext = expectedContext;
                return UserAuthTokenValidationResultCode.Success;
            });

        var firstTask = _accessor.GetUserContextAsync();
        await authStarted.Task;

        var secondTask = _accessor.GetUserContextAsync();
        releaseAuth.SetResult();

        var firstResult = await firstTask;
        var secondResult = await secondTask;

        Assert.Same(expectedContext, firstResult);
        Assert.Same(expectedContext, secondResult);
        Assert.Equal(1, authCalls);
    }
}
