using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Security.Exceptions;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.BasicAuths.Processors;

public class BasicAuthProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IBasicAuthProcessor> _mockInnerProcessor = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly BasicAuthProcessorAuthorizationDecorator _decorator;

    public BasicAuthProcessorAuthorizationDecoratorTests()
    {
        _decorator = new BasicAuthProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockUserContextProvider.Object
        );
    }

    [Fact]
    public async Task UpsertBasicAuthAsync_ThrowsUserContextNotSetException_WhenNoUserContext()
    {
        var request = new UpsertBasicAuthRequest(5, "password");
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns((UserContext?)null);

        await Assert.ThrowsAsync<UserContextNotSetException>(() =>
            _decorator.UpsertBasicAuthAsync(request)
        );

        _mockInnerProcessor.Verify(
            x => x.UpsertBasicAuthAsync(It.IsAny<UpsertBasicAuthRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpsertBasicAuthAsync_Succeeds_WhenUserOperatesOnOwnCredentials()
    {
        var userId = 5;
        var request = new UpsertBasicAuthRequest(userId, "password");
        var expectedResult = new UpsertBasicAuthResult(
            UpsertBasicAuthResultCode.Success,
            "",
            1,
            Corely.IAM.Enums.UpsertType.Update
        );
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(new UserContext(userId, 1));
        _mockInnerProcessor
            .Setup(x => x.UpsertBasicAuthAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.UpsertBasicAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.UpsertBasicAuthAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpsertBasicAuthAsync_ThrowsAuthorizationException_WhenUserOperatesOnOtherUser()
    {
        var request = new UpsertBasicAuthRequest(5, "password");
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(new UserContext(99, 1)); // Different user

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.UpsertBasicAuthAsync(request)
        );

        _mockInnerProcessor.Verify(
            x => x.UpsertBasicAuthAsync(It.IsAny<UpsertBasicAuthRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_ThrowsUserContextNotSetException_WhenNoUserContext()
    {
        var request = new VerifyBasicAuthRequest(5, "password");
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns((UserContext?)null);

        await Assert.ThrowsAsync<UserContextNotSetException>(() =>
            _decorator.VerifyBasicAuthAsync(request)
        );

        _mockInnerProcessor.Verify(
            x => x.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_Succeeds_WhenUserVerifiesOwnCredentials()
    {
        var userId = 5;
        var request = new VerifyBasicAuthRequest(userId, "password");
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(new UserContext(userId, 1));
        _mockInnerProcessor.Setup(x => x.VerifyBasicAuthAsync(request)).ReturnsAsync(true);

        var result = await _decorator.VerifyBasicAuthAsync(request);

        Assert.True(result);
        _mockInnerProcessor.Verify(x => x.VerifyBasicAuthAsync(request), Times.Once);
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_ThrowsAuthorizationException_WhenUserVerifiesOtherUser()
    {
        var request = new VerifyBasicAuthRequest(5, "password");
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(new UserContext(99, 1)); // Different user

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.VerifyBasicAuthAsync(request)
        );

        _mockInnerProcessor.Verify(
            x => x.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()),
            Times.Never
        );
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new BasicAuthProcessorAuthorizationDecorator(null!, _mockUserContextProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullUserContextProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new BasicAuthProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );
}
