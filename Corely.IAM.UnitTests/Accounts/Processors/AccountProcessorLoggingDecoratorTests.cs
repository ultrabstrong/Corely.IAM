using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Accounts.Processors;

public class AccountProcessorLoggingDecoratorTests
{
    private readonly Mock<IAccountProcessor> _mockInnerProcessor;
    private readonly Mock<ILogger<AccountProcessorLoggingDecorator>> _mockLogger;
    private readonly AccountProcessorLoggingDecorator _decorator;

    public AccountProcessorLoggingDecoratorTests()
    {
        _mockInnerProcessor = new Mock<IAccountProcessor>();
        _mockLogger = new Mock<ILogger<AccountProcessorLoggingDecorator>>();
        _decorator = new AccountProcessorLoggingDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateAccountAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new CreateAccountRequest("testaccount", Guid.CreateVersion7());
        var expectedResult = new CreateAccountResult(
            CreateAccountResultCode.Success,
            string.Empty,
            Guid.CreateVersion7()
        );
        _mockInnerProcessor.Setup(x => x.CreateAccountAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreateAccountAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetAccountAsyncById_DelegatesToInnerAndLogsResult()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new GetAccountResult(
            GetAccountResultCode.Success,
            string.Empty,
            new Account { AccountName = "testaccount" }
        );
        _mockInnerProcessor.Setup(x => x.GetAccountAsync(accountId)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.GetAccountAsync(accountId), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RemoveUserFromAccountRequest(Guid.CreateVersion7(), Guid.CreateVersion7());
        var expectedResult = new RemoveUserFromAccountResult(
            RemoveUserFromAccountResultCode.Success,
            string.Empty
        );
        _mockInnerProcessor
            .Setup(x => x.RemoveUserFromAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveUserFromAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.RemoveUserFromAccountAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AccountProcessorLoggingDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AccountProcessorLoggingDecorator(_mockInnerProcessor.Object, null!)
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
