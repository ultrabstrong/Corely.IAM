using AutoFixture;
using Corely.IAM.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Processors;

public class ProcessorBaseTests
{
    private class MockProcessorBase : ProcessorBase
    {
        public MockProcessorBase(ILogger logger)
            : base(logger) { }

        public new async Task<TResult> LogRequestResultAspect<TRequest, TResult>(
            string className,
            string methodName,
            TRequest request,
            Func<Task<TResult>> next
        ) => await base.LogRequestResultAspect(className, methodName, request, next);

        public new async Task<TResult> LogRequestAspect<TRequest, TResult>(
            string className,
            string methodName,
            TRequest request,
            Func<Task<TResult>> next
        ) => await base.LogRequestAspect(className, methodName, request, next);

        public new async Task LogRequestAspect<TRequest>(
            string className,
            string methodName,
            TRequest request,
            Func<Task> next
        ) => await base.LogRequestAspect(className, methodName, request, next);

        public new async Task<TResult> LogAspect<TResult>(
            string className,
            string methodName,
            Func<Task<TResult>> next
        ) => await base.LogAspect(className, methodName, next);

        public new async Task LogAspect(string className, string methodName, Func<Task> next) =>
            await base.LogAspect(className, methodName, next);
    }

    private const string TEST_CLASS_NAME = nameof(TEST_CLASS_NAME);
    private const string TEST_METHOD_NAME = nameof(TEST_METHOD_NAME);

    protected readonly ServiceFactory _serviceFactory = new();

    private readonly Fixture _fixture = new();
    private readonly MockProcessorBase _mockProcessorBase;

    public ProcessorBaseTests()
    {
        _mockProcessorBase = new MockProcessorBase(
            _serviceFactory.GetRequiredService<ILogger<ProcessorBaseTests>>()
        );
    }

    [Fact]
    public async Task LogRequestResultAspect_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _mockProcessorBase.LogRequestResultAspect(
                TEST_CLASS_NAME,
                TEST_METHOD_NAME,
                null! as string,
                async () => await Task.FromResult(1)
            )
        );
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task LogRequestResultAspect_ReturnsResult_WithRequestAndResult()
    {
        var result = await _mockProcessorBase.LogRequestResultAspect(
            TEST_CLASS_NAME,
            TEST_METHOD_NAME,
            string.Empty,
            async () => await Task.FromResult(1)
        );
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task LogRequestResultAspect_Throws_WhenNextThrows()
    {
        var ex = await Record.ExceptionAsync(() =>
            _mockProcessorBase.LogRequestResultAspect(
                TEST_CLASS_NAME,
                TEST_METHOD_NAME,
                string.Empty,
                async () => await Task.FromException<int>(new Exception())
            )
        );
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task LogRequestAspect_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _mockProcessorBase.LogRequestAspect(
                TEST_CLASS_NAME,
                TEST_METHOD_NAME,
                null! as string,
                () => Task.FromResult(1)
            )
        );
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task LogRequestAspect_ReturnsResult_WithRequestAndResult()
    {
        var result = await _mockProcessorBase.LogRequestAspect(
            TEST_CLASS_NAME,
            TEST_METHOD_NAME,
            string.Empty,
            () => Task.FromResult(1)
        );
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task LogRequestAspect_Throws_WhenNextThrows()
    {
        var ex = await Record.ExceptionAsync(() =>
            _mockProcessorBase.LogRequestAspect<string, int>(
                TEST_CLASS_NAME,
                TEST_METHOD_NAME,
                string.Empty,
                () => throw new Exception()
            )
        );
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task LogRequestAspectWithNoResult_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _mockProcessorBase.LogRequestAspect(
                TEST_CLASS_NAME,
                TEST_METHOD_NAME,
                null! as string,
                () => Task.CompletedTask
            )
        );
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task LogRequestAspectWithNoResult_Returns_WithRequest()
    {
        await _mockProcessorBase.LogRequestAspect(
            TEST_CLASS_NAME,
            TEST_METHOD_NAME,
            string.Empty,
            () => Task.CompletedTask
        );
    }

    [Fact]
    public async Task LogRequestAspectWithNoResult_Throws_WhenNextThrows()
    {
        var ex = await Record.ExceptionAsync(() =>
            _mockProcessorBase.LogRequestAspect(
                TEST_CLASS_NAME,
                TEST_METHOD_NAME,
                string.Empty,
                () => throw new Exception()
            )
        );
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task LogAspect_ReturnsResult()
    {
        var result = await _mockProcessorBase.LogAspect(
            TEST_CLASS_NAME,
            TEST_METHOD_NAME,
            () => Task.FromResult(1)
        );
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task LogAspect_Throws_WhenNextThrows()
    {
        var ex = await Record.ExceptionAsync(() =>
            _mockProcessorBase.LogAspect<int>(
                TEST_CLASS_NAME,
                TEST_METHOD_NAME,
                () => throw new Exception()
            )
        );
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task LogAspectWithNoResult_Throws_WhenNextThrows()
    {
        var ex = await Record.ExceptionAsync(() =>
            _mockProcessorBase.LogAspect(
                TEST_CLASS_NAME,
                TEST_METHOD_NAME,
                async () => await Task.FromException(new Exception())
            )
        );
        Assert.NotNull(ex);
    }
}
