using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class AuthenticationServiceLoggingDecoratorTests
{
    private readonly Mock<IAuthenticationService> _mockInnerService;
    private readonly Mock<ILogger<AuthenticationServiceLoggingDecorator>> _mockLogger;
    private readonly AuthenticationServiceLoggingDecorator _decorator;

    public AuthenticationServiceLoggingDecoratorTests()
    {
        _mockInnerService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<AuthenticationServiceLoggingDecorator>>();
        _decorator = new AuthenticationServiceLoggingDecorator(
            _mockInnerService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task SignInAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new SignInRequest("testuser", "password123", null);
        var expectedResult = new SignInResult(SignInResultCode.Success, null, "token123", [], 1);
        _mockInnerService.Setup(x => x.SignInAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.SignInAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.SignInAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task SignOutAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = 1;
        var tokenId = "token123";
        var expectedResult = true;
        _mockInnerService.Setup(x => x.SignOutAsync(userId, tokenId)).ReturnsAsync(expectedResult);

        var result = await _decorator.SignOutAsync(userId, tokenId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.SignOutAsync(userId, tokenId), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task SignOutAllAsync_DelegatesToInnerAndLogs()
    {
        var userId = 1;
        _mockInnerService.Setup(x => x.SignOutAllAsync(userId)).Returns(Task.CompletedTask);

        await _decorator.SignOutAllAsync(userId);

        _mockInnerService.Verify(x => x.SignOutAllAsync(userId), Times.Once);
        VerifyLoggedWithoutResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AuthenticationServiceLoggingDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AuthenticationServiceLoggingDecorator(_mockInnerService.Object, null!)
        );

    private void VerifyLoggedWithResult() =>
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("with result")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

    private void VerifyLoggedWithoutResult() =>
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("completed")
                            && !v.ToString()!.Contains("with result")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
}
