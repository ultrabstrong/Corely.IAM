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
        var request = "test-request";
        var result = "test-result";

        var actualResult = await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            request,
            () => Task.FromResult(result),
            logResult: false
        );

        Assert.Equal(result, actualResult);

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
        var request = "test-request";
        var result = "test-result";

        var actualResult = await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            request,
            () => Task.FromResult(result),
            logResult: true
        );

        Assert.Equal(result, actualResult);

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
        var request = "test-request";
        var result = "test-result";

        await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            request,
            () => Task.FromResult(result),
            logResult: false
        );

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
        var request = "test-request";
        var expectedException = new InvalidOperationException("Test exception");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockLogger.Object.ExecuteWithLoggingAsync(
                "TestClass",
                request,
                () => Task.FromException<string>(expectedException)
            )
        );

        Assert.Equal(expectedException, ex);

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
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mockLogger.Object.ExecuteWithLoggingAsync<string?, string>(
                "TestClass",
                null,
                () => Task.FromResult("result")
            )
        );
    }

    [Fact]
    public async Task ExecuteWithLogging_IncludesElapsedTime()
    {
        var request = "test-request";
        var result = "test-result";

        await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            request,
            async () =>
            {
                await Task.Delay(10);
                return result;
            },
            logResult: false
        );

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
        var request = "test-request";
        var result = "test-result";

        await ExecuteWithLoggingWrapper(request, result);

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

    [Fact]
    public async Task ExecuteWithLoggingVoid_LogsEntryAndExit()
    {
        var request = "test-request";
        var operationExecuted = false;

        await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            request,
            () =>
            {
                operationExecuted = true;
                return Task.CompletedTask;
            }
        );

        Assert.True(operationExecuted);

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

    [Fact]
    public async Task ExecuteWithLoggingVoid_LogsException_WhenOperationThrows()
    {
        var request = "test-request";
        var expectedException = new InvalidOperationException("Test exception");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockLogger.Object.ExecuteWithLoggingAsync(
                "TestClass",
                request,
                () => Task.FromException(expectedException)
            )
        );

        Assert.Equal(expectedException, ex);

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
    public async Task ExecuteWithLoggingVoid_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mockLogger.Object.ExecuteWithLoggingAsync<string?>(
                "TestClass",
                null,
                () => Task.CompletedTask
            )
        );
    }

    [Fact]
    public async Task ExecuteWithLoggingNoRequest_LogsEntryAndExit()
    {
        var result = "test-result";

        var actualResult = await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            () => Task.FromResult(result),
            logResult: false
        );

        Assert.Equal(result, actualResult);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("starting")
                            && !v.ToString()!.Contains("with request")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

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
    public async Task ExecuteWithLoggingNoRequest_LogsResult_WhenLogResultIsTrue()
    {
        var result = "test-result";

        var actualResult = await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            () => Task.FromResult(result),
            logResult: true
        );

        Assert.Equal(result, actualResult);

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
    public async Task ExecuteWithLoggingNoRequest_DoesNotLogResult_WhenLogResultIsFalse()
    {
        var result = "test-result";

        await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            () => Task.FromResult(result),
            logResult: false
        );

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
    public async Task ExecuteWithLoggingNoRequest_LogsException_WhenOperationThrows()
    {
        var expectedException = new InvalidOperationException("Test exception");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockLogger.Object.ExecuteWithLoggingAsync(
                "TestClass",
                () => Task.FromException<string>(expectedException)
            )
        );

        Assert.Equal(expectedException, ex);

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
    public async Task ExecuteWithLoggingNoRequest_IncludesElapsedTime()
    {
        var result = "test-result";

        await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            async () =>
            {
                await Task.Delay(10);
                return result;
            },
            logResult: false
        );

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
    public async Task ExecuteWithLoggingVoidNoRequest_LogsEntryAndExit()
    {
        var operationExecuted = false;

        await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            () =>
            {
                operationExecuted = true;
                return Task.CompletedTask;
            }
        );

        Assert.True(operationExecuted);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("starting")
                            && !v.ToString()!.Contains("with request")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

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

    [Fact]
    public async Task ExecuteWithLoggingVoidNoRequest_LogsException_WhenOperationThrows()
    {
        var expectedException = new InvalidOperationException("Test exception");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockLogger.Object.ExecuteWithLoggingAsync(
                "TestClass",
                () => Task.FromException(expectedException)
            )
        );

        Assert.Equal(expectedException, ex);

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
    public async Task ExecuteWithLoggingVoidNoRequest_IncludesElapsedTime()
    {
        await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            async () =>
            {
                await Task.Delay(10);
            }
        );

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

    private async Task<string> ExecuteWithLoggingWrapper(string request, string result)
    {
        return await _mockLogger.Object.ExecuteWithLoggingAsync(
            "TestClass",
            request,
            () => Task.FromResult(result)
        );
    }
}
