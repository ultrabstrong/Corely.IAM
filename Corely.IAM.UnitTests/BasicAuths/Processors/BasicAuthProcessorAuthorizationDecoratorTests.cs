using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.BasicAuths.Processors;

public class BasicAuthProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IBasicAuthProcessor> _mockInnerProcessor = new();
    private readonly Mock<IIamUserContextProvider> _mockUserContextProvider = new();
    private readonly BasicAuthProcessorAuthorizationDecorator _decorator;

    public BasicAuthProcessorAuthorizationDecoratorTests()
    {
        _decorator = new BasicAuthProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockUserContextProvider.Object
        );
    }

    [Fact]
    public async Task UpsertBasicAuthAsync_ReturnsUnauthorized_WhenNoUserContext()
    {
        var request = new UpsertBasicAuthRequest(5, "password");
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns((IamUserContext?)null);

        var result = await _decorator.UpsertBasicAuthAsync(request);

        Assert.Equal(UpsertBasicAuthResultCode.UnauthorizedError, result.ResultCode);
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
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(new IamUserContext(userId, 1));
        _mockInnerProcessor
            .Setup(x => x.UpsertBasicAuthAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.UpsertBasicAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.UpsertBasicAuthAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpsertBasicAuthAsync_ReturnsUnauthorized_WhenUserOperatesOnOtherUser()
    {
        var request = new UpsertBasicAuthRequest(5, "password");
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(new IamUserContext(99, 1));

        var result = await _decorator.UpsertBasicAuthAsync(request);

        Assert.Equal(UpsertBasicAuthResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.UpsertBasicAuthAsync(It.IsAny<UpsertBasicAuthRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_BypassesAuthorization_AndDelegatesToInner()
    {
        var request = new VerifyBasicAuthRequest(5, "password");
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
        _mockUserContextProvider.Verify(x => x.GetUserContext(), Times.Never);
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_WorksWithoutUserContext()
    {
        var request = new VerifyBasicAuthRequest(5, "password");
        var expectedResult = new VerifyBasicAuthResult(
            VerifyBasicAuthResultCode.Success,
            string.Empty,
            true
        );
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns((IamUserContext?)null);
        _mockInnerProcessor
            .Setup(x => x.VerifyBasicAuthAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.VerifyBasicAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.VerifyBasicAuthAsync(request), Times.Once);
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
