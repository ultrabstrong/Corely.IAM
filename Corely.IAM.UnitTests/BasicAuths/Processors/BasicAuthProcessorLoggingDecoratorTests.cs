using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.BasicAuths.Processors;

public class BasicAuthProcessorLoggingDecoratorTests
{
    private readonly Mock<IBasicAuthProcessor> _mockInnerProcessor;
    private readonly Mock<ILogger<BasicAuthProcessorLoggingDecorator>> _mockLogger;
    private readonly BasicAuthProcessorLoggingDecorator _decorator;

    public BasicAuthProcessorLoggingDecoratorTests()
    {
        _mockInnerProcessor = new Mock<IBasicAuthProcessor>();
        _mockLogger = new Mock<ILogger<BasicAuthProcessorLoggingDecorator>>();
        _decorator = new BasicAuthProcessorLoggingDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task UpsertBasicAuthAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new UpsertBasicAuthRequest(1, "password");
        var expectedResult = new UpsertBasicAuthResult(
            UpsertBasicAuthResultCode.Success,
            string.Empty,
            1,
            IAM.Enums.UpsertType.Create
        );
        _mockInnerProcessor
            .Setup(x => x.UpsertBasicAuthAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.UpsertBasicAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.UpsertBasicAuthAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new VerifyBasicAuthRequest(1, "password");
        var expectedResult = new VerifyBasicAuthResult(
            VerifyBasicAuthResultCode.Success,
            string.Empty,
            true
        );
        _mockInnerProcessor
            .Setup(x => x.VerifyBasicAuthAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.VerifyBasicAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.VerifyBasicAuthAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new BasicAuthProcessorLoggingDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new BasicAuthProcessorLoggingDecorator(_mockInnerProcessor.Object, null!)
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
