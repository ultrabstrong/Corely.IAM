using Corely.IAM.Invitations.Models;
using Corely.IAM.Invitations.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.UnitTests.Invitations.Processors;

public class InvitationProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IInvitationProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly InvitationProcessorAuthorizationDecorator _decorator;

    public InvitationProcessorAuthorizationDecoratorTests()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(It.IsAny<Guid>())).Returns(true);
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _decorator = new InvitationProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    [Fact]
    public async Task CreateInvitation_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new CreateInvitationRequest(
            Guid.CreateVersion7(),
            "test@test.com",
            null,
            3600
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(false);

        var result = await _decorator.CreateInvitationAsync(request);

        Assert.Equal(CreateInvitationResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.CreateInvitationAsync(It.IsAny<CreateInvitationRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CreateInvitation_DelegatesToInner_WhenAuthorized()
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
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.CreateInvitationAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.CreateInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreateInvitationAsync(request), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitation_ReturnsUnauthorized_WhenNoUserContext()
    {
        var request = new AcceptInvitationRequest("token");
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.AcceptInvitationAsync(request);

        Assert.Equal(AcceptInvitationResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.AcceptInvitationAsync(It.IsAny<AcceptInvitationRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AcceptInvitation_DelegatesToInner_WhenUserContextExists()
    {
        var request = new AcceptInvitationRequest("token");
        var expectedResult = new AcceptInvitationResult(
            AcceptInvitationResultCode.Success,
            "",
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerProcessor
            .Setup(x => x.AcceptInvitationAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AcceptInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.AcceptInvitationAsync(request), Times.Once);
    }

    [Fact]
    public async Task RevokeInvitation_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new RevokeInvitationRequest(Guid.CreateVersion7(), Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    request.AccountId
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.RevokeInvitationAsync(request);

        Assert.Equal(RevokeInvitationResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.RevokeInvitationAsync(It.IsAny<RevokeInvitationRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RevokeInvitation_DelegatesToInner_WhenAuthorized()
    {
        var request = new RevokeInvitationRequest(Guid.CreateVersion7(), Guid.CreateVersion7());
        var expectedResult = new RevokeInvitationResult(RevokeInvitationResultCode.Success, "");
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    request.AccountId
                )
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.RevokeInvitationAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RevokeInvitationAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.RevokeInvitationAsync(request), Times.Once);
    }

    [Fact]
    public async Task ListInvitations_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new ListInvitationsRequest(Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(false);

        var result = await _decorator.ListInvitationsAsync(request);

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.ListInvitationsAsync(It.IsAny<ListInvitationsRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ListInvitations_DelegatesToInner_WhenAuthorized()
    {
        var request = new ListInvitationsRequest(Guid.CreateVersion7());
        var expectedResult = new ListResult<Invitation>(RetrieveResultCode.Success, "", null);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.ListInvitationsAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListInvitationsAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.ListInvitationsAsync(request), Times.Once);
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InvitationProcessorAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InvitationProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );
}
