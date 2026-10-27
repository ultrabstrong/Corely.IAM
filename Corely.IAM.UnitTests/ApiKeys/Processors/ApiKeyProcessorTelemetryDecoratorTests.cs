using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.ApiKeys.Processors;
using Corely.IAM.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.ApiKeys.Processors;

public class ApiKeyProcessorTelemetryDecoratorTests
{
    private readonly Mock<IApiKeyProcessor> _mockInnerProcessor = new();
    private readonly Mock<ILogger<ApiKeyProcessorTelemetryDecorator>> _mockLogger = new();
    private readonly ApiKeyProcessorTelemetryDecorator _decorator;
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _apiKeyId = Guid.CreateVersion7();

    public ApiKeyProcessorTelemetryDecoratorTests()
    {
        _decorator = new ApiKeyProcessorTelemetryDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateApiKey_DelegatesToInner_WithoutLoggingTheKey()
    {
        var request = new CreateApiKeyRequest("ci", _accountId);
        var expected = new CreateApiKeyResult(
            CreateApiKeyResultCode.Success,
            "",
            _apiKeyId,
            "ciam_abcdefgh_secret"
        );
        _mockInnerProcessor.Setup(x => x.CreateApiKeyAsync(request)).ReturnsAsync(expected);

        Assert.Equal(expected, await _decorator.CreateApiKeyAsync(request));
        VerifyLoggedWithResult(Times.Never());
    }

    [Fact]
    public async Task RotateApiKeySecret_DelegatesToInner_WithoutLoggingTheKey()
    {
        var request = new RotateApiKeySecretRequest(_apiKeyId, _accountId, ApiKeySlot.Primary);
        var expected = new RotateApiKeySecretResult(
            RotateApiKeySecretResultCode.Success,
            "",
            "ciam_abcdefgh_secret"
        );
        _mockInnerProcessor.Setup(x => x.RotateApiKeySecretAsync(request)).ReturnsAsync(expected);

        Assert.Equal(expected, await _decorator.RotateApiKeySecretAsync(request));
        VerifyLoggedWithResult(Times.Never());
    }

    [Fact]
    public async Task UpdateApiKey_DelegatesToInnerAndLogsResult()
    {
        var request = new UpdateApiKeyRequest(_apiKeyId, _accountId, "ci", null);
        _mockInnerProcessor
            .Setup(x => x.UpdateApiKeyAsync(request))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, ""));

        await _decorator.UpdateApiKeyAsync(request);

        _mockInnerProcessor.Verify(x => x.UpdateApiKeyAsync(request), Times.Once);
        VerifyLoggedWithResult(Times.Once());
    }

    [Fact]
    public async Task DeleteApiKey_DelegatesToInnerAndLogsResult()
    {
        _mockInnerProcessor
            .Setup(x => x.DeleteApiKeyAsync(_apiKeyId, _accountId))
            .ReturnsAsync(new DeleteApiKeyResult(DeleteApiKeyResultCode.Success, ""));

        await _decorator.DeleteApiKeyAsync(_apiKeyId, _accountId);

        _mockInnerProcessor.Verify(x => x.DeleteApiKeyAsync(_apiKeyId, _accountId), Times.Once);
        VerifyLoggedWithResult(Times.Once());
    }

    [Fact]
    public async Task AssignRolesToApiKey_DelegatesToInnerAndLogsResult()
    {
        var request = new AssignRolesToApiKeyRequest(
            [Guid.CreateVersion7()],
            _apiKeyId,
            _accountId
        );
        _mockInnerProcessor
            .Setup(x => x.AssignRolesToApiKeyAsync(request))
            .ReturnsAsync(
                new AssignRolesToApiKeyResult(AssignRolesToApiKeyResultCode.Success, "", 1, [])
            );

        await _decorator.AssignRolesToApiKeyAsync(request);

        _mockInnerProcessor.Verify(x => x.AssignRolesToApiKeyAsync(request), Times.Once);
        VerifyLoggedWithResult(Times.Once());
    }

    [Fact]
    public async Task RemoveRolesFromApiKey_DelegatesToInnerAndLogsResult()
    {
        var request = new RemoveRolesFromApiKeyRequest(
            [Guid.CreateVersion7()],
            _apiKeyId,
            _accountId
        );
        _mockInnerProcessor
            .Setup(x => x.RemoveRolesFromApiKeyAsync(request))
            .ReturnsAsync(
                new RemoveRolesFromApiKeyResult(RemoveRolesFromApiKeyResultCode.Success, "", 1, [])
            );

        await _decorator.RemoveRolesFromApiKeyAsync(request);

        _mockInnerProcessor.Verify(x => x.RemoveRolesFromApiKeyAsync(request), Times.Once);
        VerifyLoggedWithResult(Times.Once());
    }

    [Fact]
    public async Task ListAndGet_DelegateToInnerAndLogResults()
    {
        var request = new ListApiKeysRequest(_accountId);
        _mockInnerProcessor
            .Setup(x => x.ListApiKeysAsync(request, null))
            .ReturnsAsync(new ListResult<ApiKey>(RetrieveResultCode.Success, "", null));
        _mockInnerProcessor
            .Setup(x => x.GetApiKeyByIdAsync(_apiKeyId, false, _accountId))
            .ReturnsAsync(new GetResult<ApiKey>(RetrieveResultCode.Success, "", null));

        await _decorator.ListApiKeysAsync(request);
        await _decorator.GetApiKeyByIdAsync(_apiKeyId, false, _accountId);

        _mockInnerProcessor.Verify(x => x.ListApiKeysAsync(request, null), Times.Once);
        _mockInnerProcessor.Verify(
            x => x.GetApiKeyByIdAsync(_apiKeyId, false, _accountId),
            Times.Once
        );
        VerifyLoggedWithResult(Times.Exactly(2));
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ApiKeyProcessorTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ApiKeyProcessorTelemetryDecorator(_mockInnerProcessor.Object, null!)
        );

    private void VerifyLoggedWithResult(Times times) =>
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("with result")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            times
        );
}
