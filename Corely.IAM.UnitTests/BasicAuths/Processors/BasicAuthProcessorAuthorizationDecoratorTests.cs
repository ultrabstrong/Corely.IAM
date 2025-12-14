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
    public async Task UpsertBasicAuthAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var request = new UpsertBasicAuthRequest(5, "password");
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(request.UserId))
            .Returns(false);

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
        _mockAuthorizationProvider.Setup(x => x.IsAuthorizedForOwnUser(userId)).Returns(true);
        _mockInnerProcessor
            .Setup(x => x.UpsertBasicAuthAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.UpsertBasicAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.UpsertBasicAuthAsync(request), Times.Once);
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
        // Should not call any authorization methods
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedForOwnUser(It.IsAny<int>()),
            Times.Never
        );
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<int?>()),
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
