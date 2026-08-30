using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class InvitationServiceTelemetryDecoratorTests
{
    private readonly Mock<IInvitationService> _mockInnerService = new();
    private readonly Mock<ILogger<InvitationServiceTelemetryDecorator>> _mockLogger = new();
    private readonly InvitationServiceTelemetryDecorator _decorator;

    public InvitationServiceTelemetryDecoratorTests()
    {
        _decorator = new InvitationServiceTelemetryDecorator(
            _mockInnerService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InvitationServiceTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InvitationServiceTelemetryDecorator(_mockInnerService.Object, null!)
        );

    [Fact]
    public async Task CreateInvitation_DelegatesToInnerAndLogsResult()
    {
        var request = new CreateInvitationRequest(Guid.NewGuid(), "user@example.com", "desc", 3600);
        var expectedResult = new CreateInvitationResult(
            CreateInvitationResultCode.Success,
            string.Empty,
            "token",
            Guid.NewGuid()
        );
        _mockInnerService.Setup(x => x.CreateInvitationAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.CreateInvitationAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task AcceptInvitation_DelegatesToInnerAndLogsResult()
    {
        var request = new AcceptInvitationRequest("token");
        var expectedResult = new AcceptInvitationResult(
            AcceptInvitationResultCode.Success,
            string.Empty,
            Guid.NewGuid()
        );
        _mockInnerService.Setup(x => x.AcceptInvitationAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.AcceptInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.AcceptInvitationAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task AcceptInvitation_LogsFailureResult()
    {
        var request = new AcceptInvitationRequest("expired");
        var expectedResult = new AcceptInvitationResult(
            AcceptInvitationResultCode.InvitationExpiredError,
            "expired",
            null
        );
        _mockInnerService.Setup(x => x.AcceptInvitationAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.AcceptInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RevokeInvitation_DelegatesToInnerAndLogsResult()
    {
        var request = new RevokeInvitationRequest(Guid.NewGuid(), Guid.NewGuid());
        var expectedResult = new RevokeInvitationResult(
            RevokeInvitationResultCode.Success,
            string.Empty
        );
        _mockInnerService.Setup(x => x.RevokeInvitationAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RevokeInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RevokeInvitationAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ListInvitations_DelegatesToInnerAndLogsResult()
    {
        var request = new ListInvitationsRequest(Guid.NewGuid());
        var expectedResult = new RetrieveListResult<Invitation>(
            RetrieveResultCode.Success,
            string.Empty,
            PagedResult<Invitation>.Empty()
        );
        _mockInnerService.Setup(x => x.ListInvitationsAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ListInvitationsAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListInvitationsAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task CreateInvitation_DoesNotSwallowExceptions()
    {
        var request = new CreateInvitationRequest(Guid.NewGuid(), "user@example.com", null, 3600);
        _mockInnerService
            .Setup(x => x.CreateInvitationAsync(request))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _decorator.CreateInvitationAsync(request)
        );
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
