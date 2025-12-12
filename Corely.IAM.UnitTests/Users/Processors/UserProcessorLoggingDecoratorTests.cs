using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Users.Processors;

public class UserProcessorLoggingDecoratorTests
{
    private readonly Mock<IUserProcessor> _mockInnerProcessor;
    private readonly Mock<ILogger<UserProcessorLoggingDecorator>> _mockLogger;
    private readonly UserProcessorLoggingDecorator _decorator;

    public UserProcessorLoggingDecoratorTests()
    {
        _mockInnerProcessor = new Mock<IUserProcessor>();
        _mockLogger = new Mock<ILogger<UserProcessorLoggingDecorator>>();
        _decorator = new UserProcessorLoggingDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateUserAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new CreateUserRequest("testuser", "test@example.com");
        var expectedResult = new CreateUserResult(CreateUserResultCode.Success, string.Empty, 1);
        _mockInnerProcessor.Setup(x => x.CreateUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreateUserAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetUserAsyncById_DelegatesToInnerWithoutLoggingResult()
    {
        var userId = 1;
        var expectedUser = new User { Username = "testuser" };
        _mockInnerProcessor.Setup(x => x.GetUserAsync(userId)).ReturnsAsync(expectedUser);

        var result = await _decorator.GetUserAsync(userId);

        Assert.Equal(expectedUser, result);
        _mockInnerProcessor.Verify(x => x.GetUserAsync(userId), Times.Once);
        VerifyLoggedWithoutResult();
    }

    [Fact]
    public async Task GetUserAsyncByName_DelegatesToInnerWithoutLoggingResult()
    {
        var userName = "testuser";
        var expectedUser = new User { Username = userName };
        _mockInnerProcessor.Setup(x => x.GetUserAsync(userName)).ReturnsAsync(expectedUser);

        var result = await _decorator.GetUserAsync(userName);

        Assert.Equal(expectedUser, result);
        _mockInnerProcessor.Verify(x => x.GetUserAsync(userName), Times.Once);
        VerifyLoggedWithoutResult();
    }

    [Fact]
    public async Task UpdateUserAsync_DelegatesToInner()
    {
        var user = new User { Username = "testuser" };
        _mockInnerProcessor.Setup(x => x.UpdateUserAsync(user)).Returns(Task.CompletedTask);

        await _decorator.UpdateUserAsync(user);

        _mockInnerProcessor.Verify(x => x.UpdateUserAsync(user), Times.Once);
        VerifyLogged();
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_DelegatesToInnerWithoutLoggingResult()
    {
        var userId = 1;
        var request = new UserAuthTokenRequest(userId);
        var expectedResult = new UserAuthTokenResult("test-token", [], null);
        _mockInnerProcessor
            .Setup(x => x.GetUserAuthTokenAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserAuthTokenAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.GetUserAuthTokenAsync(request), Times.Once);
        VerifyLoggedWithoutResult();
    }

    [Fact]
    public async Task IsUserAuthTokenValidAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = 1;
        var authToken = "test-token";
        _mockInnerProcessor
            .Setup(x => x.IsUserAuthTokenValidAsync(userId, authToken))
            .ReturnsAsync(true);

        var result = await _decorator.IsUserAuthTokenValidAsync(userId, authToken);

        Assert.True(result);
        _mockInnerProcessor.Verify(x => x.IsUserAuthTokenValidAsync(userId, authToken), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RevokeUserAuthTokenAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = 1;
        var jti = "test-jti";
        _mockInnerProcessor.Setup(x => x.RevokeUserAuthTokenAsync(userId, jti)).ReturnsAsync(true);

        var result = await _decorator.RevokeUserAuthTokenAsync(userId, jti);

        Assert.True(result);
        _mockInnerProcessor.Verify(x => x.RevokeUserAuthTokenAsync(userId, jti), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RevokeAllUserAuthTokensAsync_DelegatesToInner()
    {
        var userId = 1;
        _mockInnerProcessor
            .Setup(x => x.RevokeAllUserAuthTokensAsync(userId))
            .Returns(Task.CompletedTask);

        await _decorator.RevokeAllUserAuthTokensAsync(userId);

        _mockInnerProcessor.Verify(x => x.RevokeAllUserAuthTokensAsync(userId), Times.Once);
        VerifyLogged();
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = 1;
        var expectedKey = "test-key";
        _mockInnerProcessor
            .Setup(x => x.GetAsymmetricSignatureVerificationKeyAsync(userId))
            .ReturnsAsync(expectedKey);

        var result = await _decorator.GetAsymmetricSignatureVerificationKeyAsync(userId);

        Assert.Equal(expectedKey, result);
        _mockInnerProcessor.Verify(
            x => x.GetAsymmetricSignatureVerificationKeyAsync(userId),
            Times.Once
        );
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task AssignRolesToUserAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new AssignRolesToUserRequest([1, 2], 1);
        var expectedResult = new AssignRolesToUserResult(
            AssignRolesToUserResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.AssignRolesToUserAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AssignRolesToUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.AssignRolesToUserAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new UserProcessorLoggingDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new UserProcessorLoggingDecorator(_mockInnerProcessor.Object, null!)
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

    private void VerifyLogged() =>
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
}
