using Corely.IAM.Invitations.Models;
using Corely.IAM.Invitations.Processors;
using Corely.IAM.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Invitations.Processors;

public class InvitationProcessorTelemetryDecoratorTests
{
    private readonly Mock<IInvitationProcessor> _mockInnerProcessor;
    private readonly Mock<ILogger<InvitationProcessorTelemetryDecorator>> _mockLogger;
    private readonly InvitationProcessorTelemetryDecorator _decorator;

    public InvitationProcessorTelemetryDecoratorTests()
    {
        _mockInnerProcessor = new Mock<IInvitationProcessor>();
        _mockLogger = new Mock<ILogger<InvitationProcessorTelemetryDecorator>>();
        _decorator = new InvitationProcessorTelemetryDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateInvitation_DelegatesToInnerAndLogsResult()
    {
        var request = new CreateInvitationRequest(
            Guid.CreateVersion7(),
            "test@test.com",
            null,
            3600
        );
        var expectedResult = new CreateInvitationResult(
            CreateInvitationResultCode.Success,
            "",
            "token",
            Guid.CreateVersion7()
        );
        _mockInnerProcessor
            .Setup(x => x.CreateInvitationAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.CreateInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreateInvitationAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task AcceptInvitation_DelegatesToInnerAndLogsResult()
    {
        var request = new AcceptInvitationRequest("token");
        var expectedResult = new AcceptInvitationResult(
            AcceptInvitationResultCode.Success,
            "",
            Guid.CreateVersion7()
        );
        _mockInnerProcessor
            .Setup(x => x.AcceptInvitationAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AcceptInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.AcceptInvitationAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RevokeInvitation_DelegatesToInnerAndLogsResult()
    {
        var request = new RevokeInvitationRequest(Guid.CreateVersion7(), Guid.CreateVersion7());
        var expectedResult = new RevokeInvitationResult(RevokeInvitationResultCode.Success, "");
        _mockInnerProcessor
            .Setup(x => x.RevokeInvitationAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RevokeInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.RevokeInvitationAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ListInvitations_DelegatesToInnerAndLogsResult()
    {
        var request = new ListInvitationsRequest(Guid.CreateVersion7());
        var expectedResult = new ListResult<Invitation>(RetrieveResultCode.Success, "", null);
        _mockInnerProcessor
            .Setup(x => x.ListInvitationsAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListInvitationsAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.ListInvitationsAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InvitationProcessorTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InvitationProcessorTelemetryDecorator(_mockInnerProcessor.Object, null!)
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
