using Corely.IAM.Extensions;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Extensions;

public class LoggerExtensionsTests
{
    private readonly Mock<ILogger> _mockLogger;

    public LoggerExtensionsTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public async Task ExecuteWithLogging_LogsEntryAndExit()
    {
        // Arrange
        var request = "test-request";
        var result = "test-result";

        // Act
        var actualResult = await _mockLogger.Object.ExecuteWithLogging(
            "TestClass",
            request,
            () => Task.FromResult(result),
            logResult: false
        );

        // Assert
        Assert.Equal(result, actualResult);

        // Verify entry log
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("starting with request")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        // Verify exit log (without result)
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

    [Fact]
    public async Task ExecuteWithLogging_LogsResult_WhenLogResultIsTrue()
    {
        // Arrange
        var request = "test-request";
        var result = "test-result";

        // Act
        var actualResult = await _mockLogger.Object.ExecuteWithLogging(
            "TestClass",
            request,
            () => Task.FromResult(result),
            logResult: true
        );

        // Assert
        Assert.Equal(result, actualResult);

        // Verify exit log includes result
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

    [Fact]
    public async Task ExecuteWithLogging_DoesNotLogResult_WhenLogResultIsFalse()
    {
        // Arrange
        var request = "test-request";
        var result = "test-result";

        // Act
        await _mockLogger.Object.ExecuteWithLogging(
            "TestClass",
            request,
            () => Task.FromResult(result),
            logResult: false
        );

        // Assert - verify "with result" message is NOT logged
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("completed")
                            && v.ToString()!.Contains("with result")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task ExecuteWithLogging_LogsException_WhenOperationThrows()
    {
        // Arrange
        var request = "test-request";
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockLogger.Object.ExecuteWithLogging(
                "TestClass",
                request,
                () => Task.FromException<string>(expectedException)
            )
        );

        Assert.Equal(expectedException, ex);

        // Verify error log
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed")),
                    It.Is<Exception>(e => e == expectedException),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteWithLogging_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mockLogger.Object.ExecuteWithLogging<string?, string>(
                "TestClass",
                null,
                () => Task.FromResult("result")
            )
        );
    }

    [Fact]
    public async Task ExecuteWithLogging_IncludesElapsedTime()
    {
        // Arrange
        var request = "test-request";
        var result = "test-result";

        // Act
        await _mockLogger.Object.ExecuteWithLogging(
            "TestClass",
            request,
            async () =>
            {
                await Task.Delay(10); // Small delay to ensure measurable time
                return result;
            },
            logResult: false
        );

        // Assert - verify timing is logged
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ms")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task ExecuteWithLogging_UsesCallerMemberName()
    {
        // Arrange
        var request = "test-request";
        var result = "test-result";

        // Act
        await ExecuteWithLoggingWrapper(request, result);

        // Assert - verify method name is captured
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("ExecuteWithLoggingWrapper")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    private async Task<string> ExecuteWithLoggingWrapper(string request, string result)
    {
        return await _mockLogger.Object.ExecuteWithLogging(
            "TestClass",
            request,
            () => Task.FromResult(result)
        );
    }
}
