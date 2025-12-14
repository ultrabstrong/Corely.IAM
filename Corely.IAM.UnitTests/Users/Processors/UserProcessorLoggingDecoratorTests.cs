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
        var expectedResult = new CreateUserResult(
            CreateUserResultCode.Success,
            string.Empty,
            1,
            Guid.NewGuid()
        );
        _mockInnerProcessor.Setup(x => x.CreateUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreateUserAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetUserAsyncById_DelegatesToInnerAndLogsResult()
    {
        var userId = 1;
        var expectedResult = new GetUserResult(
            GetUserResultCode.Success,
            string.Empty,
            new User { Username = "testuser" }
        );
        _mockInnerProcessor.Setup(x => x.GetUserAsync(userId)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.GetUserAsync(userId), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetUserAsyncByName_DelegatesToInnerAndLogsResult()
    {
        var userName = "testuser";
        var expectedResult = new GetUserResult(
            GetUserResultCode.Success,
            string.Empty,
            new User { Username = userName }
        );
        _mockInnerProcessor.Setup(x => x.GetUserAsync(userName)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserAsync(userName);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.GetUserAsync(userName), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task UpdateUserAsync_DelegatesToInnerAndLogsResult()
    {
        var user = new User { Username = "testuser" };
        var expectedResult = new UpdateUserResult(UpdateUserResultCode.Success, string.Empty);
        _mockInnerProcessor.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(expectedResult);

        var result = await _decorator.UpdateUserAsync(user);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.UpdateUserAsync(user), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = 1;
        var expectedResult = new GetAsymmetricKeyResult(
            GetAsymmetricKeyResultCode.Success,
            string.Empty,
            "test-key"
        );
        _mockInnerProcessor
            .Setup(x => x.GetAsymmetricSignatureVerificationKeyAsync(userId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAsymmetricSignatureVerificationKeyAsync(userId);

        Assert.Equal(expectedResult, result);
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
    public async Task RemoveRolesFromUserAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RemoveRolesFromUserRequest([1, 2], 1);
        var expectedResult = new RemoveRolesFromUserResult(
            RemoveRolesFromUserResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.RemoveRolesFromUserAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveRolesFromUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.RemoveRolesFromUserAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DeleteUserAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = 1;
        var expectedResult = new DeleteUserResult(DeleteUserResultCode.Success, string.Empty);
        _mockInnerProcessor.Setup(x => x.DeleteUserAsync(userId)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteUserAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.DeleteUserAsync(userId), Times.Once);
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
}
