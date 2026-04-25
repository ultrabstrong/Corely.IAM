using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;

namespace Corely.IAM.UnitTests.Services;

public class InvitationServiceAuthorizationDecoratorTests
{
    private readonly Mock<IInvitationService> _mockInnerService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly InvitationServiceAuthorizationDecorator _decorator;

    public InvitationServiceAuthorizationDecoratorTests()
    {
        _decorator = new InvitationServiceAuthorizationDecorator(
            _mockInnerService.Object,
            _mockAuthorizationProvider.Object
        );
    }

    #region CreateInvitationAsync

    [Fact]
    public async Task CreateInvitation_DelegatesToInner_WhenHasAccountContext()
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
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(It.IsAny<Guid>())).Returns(true);
        _mockInnerService.Setup(x => x.CreateInvitationAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.CreateInvitationAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateInvitation_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new CreateInvitationRequest(
            Guid.CreateVersion7(),
            "test@test.com",
            null,
            3600
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(It.IsAny<Guid>())).Returns(false);

        var result = await _decorator.CreateInvitationAsync(request);

        Assert.Equal(CreateInvitationResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.CreateInvitationAsync(It.IsAny<CreateInvitationRequest>()),
            Times.Never
        );
    }

    #endregion

    #region AcceptInvitationAsync

    [Fact]
    public async Task AcceptInvitation_DelegatesToInner_WhenHasUserContext()
    {
        var request = new AcceptInvitationRequest("token");
        var expectedResult = new AcceptInvitationResult(
            AcceptInvitationResultCode.Success,
            "",
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.AcceptInvitationAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.AcceptInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.AcceptInvitationAsync(request), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitation_ReturnsUnauthorized_WhenNoUserContext()
    {
        var request = new AcceptInvitationRequest("token");
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.AcceptInvitationAsync(request);

        Assert.Equal(AcceptInvitationResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.AcceptInvitationAsync(It.IsAny<AcceptInvitationRequest>()),
            Times.Never
        );
    }

    #endregion

    #region RevokeInvitationAsync

    [Fact]
    public async Task RevokeInvitation_DelegatesToInner_WhenHasAccountContext()
    {
        var request = new RevokeInvitationRequest(Guid.CreateVersion7(), Guid.CreateVersion7());
        var expectedResult = new RevokeInvitationResult(RevokeInvitationResultCode.Success, "");
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(It.IsAny<Guid>())).Returns(true);
        _mockInnerService.Setup(x => x.RevokeInvitationAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RevokeInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RevokeInvitationAsync(request), Times.Once);
    }

    [Fact]
    public async Task RevokeInvitation_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RevokeInvitationRequest(Guid.CreateVersion7(), Guid.CreateVersion7());
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(It.IsAny<Guid>())).Returns(false);

        var result = await _decorator.RevokeInvitationAsync(request);

        Assert.Equal(RevokeInvitationResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.RevokeInvitationAsync(It.IsAny<RevokeInvitationRequest>()),
            Times.Never
        );
    }

    #endregion

    #region ListInvitationsAsync

    [Fact]
    public async Task ListInvitations_DelegatesToInner_WhenHasAccountContext()
    {
        var request = new ListInvitationsRequest(Guid.CreateVersion7());
        var expectedResult = new RetrieveListResult<Invitation>(
            RetrieveResultCode.Success,
            "",
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(It.IsAny<Guid>())).Returns(true);
        _mockInnerService.Setup(x => x.ListInvitationsAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ListInvitationsAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListInvitationsAsync(request), Times.Once);
    }

    [Fact]
    public async Task ListInvitations_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new ListInvitationsRequest(Guid.CreateVersion7());
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(It.IsAny<Guid>())).Returns(false);

        var result = await _decorator.ListInvitationsAsync(request);

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ListInvitationsAsync(It.IsAny<ListInvitationsRequest>()),
            Times.Never
        );
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InvitationServiceAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InvitationServiceAuthorizationDecorator(_mockInnerService.Object, null!)
        );

    #endregion
}
