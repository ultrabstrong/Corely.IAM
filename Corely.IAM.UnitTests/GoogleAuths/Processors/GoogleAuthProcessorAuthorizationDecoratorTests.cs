using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.UnitTests.GoogleAuths.Processors;

public class GoogleAuthProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IGoogleAuthProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly GoogleAuthProcessorAuthorizationDecorator _decorator;

    public GoogleAuthProcessorAuthorizationDecoratorTests()
    {
        _decorator = new GoogleAuthProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    [Fact]
    public async Task LinkGoogleAuthAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.LinkGoogleAuthAsync(userId, "token");

        Assert.Equal(LinkGoogleAuthResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task LinkGoogleAuthAsync_Delegates_WhenAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        var expected = new LinkGoogleAuthResult(LinkGoogleAuthResultCode.Success, string.Empty);
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(true);
        _mockInnerProcessor
            .Setup(x => x.LinkGoogleAuthAsync(userId, "token"))
            .ReturnsAsync(expected);

        var result = await _decorator.LinkGoogleAuthAsync(userId, "token");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UnlinkGoogleAuthAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.UnlinkGoogleAuthAsync(userId);

        Assert.Equal(UnlinkGoogleAuthResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task GetAuthMethodsAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.GetAuthMethodsAsync(userId);

        Assert.Equal(AuthMethodsResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task GetUserIdByGoogleSubjectAsync_BypassesAuthorization_AndDelegates()
    {
        var expectedUserId = Guid.CreateVersion7();
        _mockInnerProcessor
            .Setup(x => x.GetUserIdByGoogleSubjectAsync("subject"))
            .ReturnsAsync(expectedUserId);

        var result = await _decorator.GetUserIdByGoogleSubjectAsync("subject");

        Assert.Equal(expectedUserId, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedForOwnUser(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GoogleAuthProcessorAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GoogleAuthProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );
}
