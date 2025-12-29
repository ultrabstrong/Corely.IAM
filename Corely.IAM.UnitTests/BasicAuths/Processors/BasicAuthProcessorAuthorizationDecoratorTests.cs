using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.UnitTests.BasicAuths.Processors;

public class BasicAuthProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IBasicAuthProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly BasicAuthProcessorAuthorizationDecorator _decorator;

    public BasicAuthProcessorAuthorizationDecoratorTests()
    {
        _decorator = new BasicAuthProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    [Fact]
    public async Task CreateBasicAuthAsync_BypassesAuthorization_AndDelegatesToInner()
    {
        var request = new CreateBasicAuthRequest(Guid.CreateVersion7(), "password");
        var expectedResult = new CreateBasicAuthResult(CreateBasicAuthResultCode.Success, "", Guid.CreateVersion7());
        _mockInnerProcessor
            .Setup(x => x.CreateBasicAuthAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.CreateBasicAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreateBasicAuthAsync(request), Times.Once);
        // Should not call any authorization methods
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedForOwnUser(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateBasicAuthAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var request = new UpdateBasicAuthRequest(Guid.CreateVersion7(), "password");
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(request.UserId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.UpdateBasicAuthAsync(request);

        Assert.Equal(UpdateBasicAuthResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.UpdateBasicAuthAsync(It.IsAny<UpdateBasicAuthRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateBasicAuthAsync_Succeeds_WhenUserOperatesOnOwnCredentials()
    {
        var userId = Guid.CreateVersion7();
        var request = new UpdateBasicAuthRequest(userId, "password");
        var expectedResult = new UpdateBasicAuthResult(UpdateBasicAuthResultCode.Success, "");
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(true);
        _mockInnerProcessor
            .Setup(x => x.UpdateBasicAuthAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.UpdateBasicAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.UpdateBasicAuthAsync(request), Times.Once);
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_BypassesAuthorization_AndDelegatesToInner()
    {
        var request = new VerifyBasicAuthRequest(Guid.CreateVersion7(), "password");
        var expectedResult = new VerifyBasicAuthResult(
            VerifyBasicAuthResultCode.Success,
            string.Empty,
            true
        );
        _mockInnerProcessor
            .Setup(x => x.VerifyBasicAuthAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.VerifyBasicAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.VerifyBasicAuthAsync(request), Times.Once);
        // Should not call any authorization methods
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedForOwnUser(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>()),
            Times.Never
        );
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new BasicAuthProcessorAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new BasicAuthProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );
}
