using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Security.Processors;

public class KeyRotationProcessorTelemetryDecoratorTests
{
    private readonly Mock<IKeyRotationProcessor> _mockInnerProcessor = new();
    private readonly Mock<ILogger<KeyRotationProcessorTelemetryDecorator>> _mockLogger = new();
    private readonly KeyRotationProcessorTelemetryDecorator _decorator;

    public KeyRotationProcessorTelemetryDecoratorTests()
    {
        _decorator = new KeyRotationProcessorTelemetryDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task RotateAccountKey_DelegatesToInnerAndLogsResult()
    {
        var request = new RotateAccountKeyRequest(
            Guid.CreateVersion7(),
            KeyType.SymmetricEncryption
        );
        var expected = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockInnerProcessor.Setup(x => x.RotateAccountKeyAsync(request)).ReturnsAsync(expected);

        var result = await _decorator.RotateAccountKeyAsync(request);

        Assert.Same(expected, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RotateCurrentUserKey_DelegatesToInnerAndLogsResult()
    {
        var expected = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockInnerProcessor
            .Setup(x => x.RotateCurrentUserKeyAsync(KeyType.AsymmetricEncryption))
            .ReturnsAsync(expected);

        var result = await _decorator.RotateCurrentUserKeyAsync(KeyType.AsymmetricEncryption);

        Assert.Same(expected, result);
        VerifyLoggedWithResult();
    }

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
