using Corely.IAM.Invitations.Models;
using Corely.IAM.Invitations.Processors;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.UnitTests.Services;

public class InvitationServiceTests
{
    private readonly Mock<IInvitationProcessor> _invitationProcessorMock = new();
    private readonly InvitationService _service;

    public InvitationServiceTests()
    {
        _service = new InvitationService(_invitationProcessorMock.Object);
    }

    [Fact]
    public void Constructor_ThrowsOnNullProcessor() =>
        Assert.Throws<ArgumentNullException>(() => new InvitationService(null!));

    [Fact]
    public async Task CreateInvitationAsync_DelegatesToProcessor()
    {
        var request = new CreateInvitationRequest(Guid.NewGuid(), "user@example.com", "desc", 3600);
        var expected = new CreateInvitationResult(
            CreateInvitationResultCode.Success,
            string.Empty,
            "token",
            Guid.NewGuid()
        );
        _invitationProcessorMock
            .Setup(x => x.CreateInvitationAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.CreateInvitationAsync(request);

        Assert.Equal(expected, result);
        _invitationProcessorMock.Verify(x => x.CreateInvitationAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateInvitationAsync_ReturnsFailureResultUnchanged()
    {
        var request = new CreateInvitationRequest(Guid.NewGuid(), "user@example.com", null, 3600);
        var expected = new CreateInvitationResult(
            CreateInvitationResultCode.UserAlreadyInAccountError,
            "already a member",
            null,
            null
        );
        _invitationProcessorMock
            .Setup(x => x.CreateInvitationAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.CreateInvitationAsync(request);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AcceptInvitationAsync_DelegatesToProcessor()
    {
        var request = new AcceptInvitationRequest("token");
        var expected = new AcceptInvitationResult(
            AcceptInvitationResultCode.Success,
            string.Empty,
            Guid.NewGuid()
        );
        _invitationProcessorMock
            .Setup(x => x.AcceptInvitationAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.AcceptInvitationAsync(request);

        Assert.Equal(expected, result);
        _invitationProcessorMock.Verify(x => x.AcceptInvitationAsync(request), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsFailureResultUnchanged()
    {
        var request = new AcceptInvitationRequest("expired-token");
        var expected = new AcceptInvitationResult(
            AcceptInvitationResultCode.InvitationExpiredError,
            "expired",
            null
        );
        _invitationProcessorMock
            .Setup(x => x.AcceptInvitationAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.AcceptInvitationAsync(request);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task RevokeInvitationAsync_DelegatesToProcessor()
    {
        var request = new RevokeInvitationRequest(Guid.NewGuid(), Guid.NewGuid());
        var expected = new RevokeInvitationResult(RevokeInvitationResultCode.Success, string.Empty);
        _invitationProcessorMock
            .Setup(x => x.RevokeInvitationAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.RevokeInvitationAsync(request);

        Assert.Equal(expected, result);
        _invitationProcessorMock.Verify(x => x.RevokeInvitationAsync(request), Times.Once);
    }

    [Fact]
    public async Task RevokeInvitationAsync_ReturnsFailureResultUnchanged()
    {
        var request = new RevokeInvitationRequest(Guid.NewGuid(), Guid.NewGuid());
        var expected = new RevokeInvitationResult(
            RevokeInvitationResultCode.InvitationAlreadyAcceptedError,
            "already accepted"
        );
        _invitationProcessorMock
            .Setup(x => x.RevokeInvitationAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.RevokeInvitationAsync(request);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ListInvitationsAsync_MapsProcessorResultToRetrieveListResult()
    {
        var request = new ListInvitationsRequest(Guid.NewGuid());
        var invitations = new List<Invitation> { new() { Id = Guid.NewGuid() } };
        var paged = PagedResult<Invitation>.Create(invitations, 1, 0, 25);
        _invitationProcessorMock
            .Setup(x => x.ListInvitationsAsync(request))
            .ReturnsAsync(new ListResult<Invitation>(RetrieveResultCode.Success, "ok", paged));

        var result = await _service.ListInvitationsAsync(request);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.Equal("ok", result.Message);
        Assert.Same(paged, result.Data);
    }

    [Fact]
    public async Task ListInvitationsAsync_PreservesNullData()
    {
        var request = new ListInvitationsRequest(Guid.NewGuid());
        _invitationProcessorMock
            .Setup(x => x.ListInvitationsAsync(request))
            .ReturnsAsync(
                new ListResult<Invitation>(RetrieveResultCode.NotFoundError, "not found", null)
            );

        var result = await _service.ListInvitationsAsync(request);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Equal("not found", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task ListInvitationsAsync_PreservesNonSuccessResultCode()
    {
        var request = new ListInvitationsRequest(Guid.NewGuid());
        _invitationProcessorMock
            .Setup(x => x.ListInvitationsAsync(request))
            .ReturnsAsync(
                new ListResult<Invitation>(
                    RetrieveResultCode.UnauthorizedError,
                    "unauthorized",
                    PagedResult<Invitation>.Empty()
                )
            );

        var result = await _service.ListInvitationsAsync(request);

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        Assert.Equal("unauthorized", result.Message);
        Assert.Empty(result.Data!.Items);
    }
}
