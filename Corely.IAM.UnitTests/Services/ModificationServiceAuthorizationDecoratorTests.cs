using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Services;

public class ModificationServiceAuthorizationDecoratorTests
{
    private readonly Mock<IModificationService> _mockInnerService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly ModificationServiceAuthorizationDecorator _decorator;

    public ModificationServiceAuthorizationDecoratorTests()
    {
        _decorator = new ModificationServiceAuthorizationDecorator(
            _mockInnerService.Object,
            _mockAuthorizationProvider.Object
        );
    }

    [Fact]
    public async Task ModifyPassword_DelegatesToInner_WhenNonSystemUserContext()
    {
        var request = new ModifyPasswordRequest("current", "new");
        var expectedResult = new ModifyPasswordResult(ModifyPasswordResultCode.Success, "");
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.ModifyPasswordAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ModifyPasswordAsync(request);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ModifyPassword_ReturnsUnauthorized_WhenNoUserContextOrSystemContext()
    {
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.ModifyPasswordAsync(new("current", "new"));

        Assert.Equal(ModifyPasswordResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ModifyPasswordAsync(It.IsAny<ModifyPasswordRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task OtherMethods_PassStraightThrough_WithoutCheckingContext()
    {
        var request = new UpdateUserRequest(Guid.CreateVersion7(), "user", "user@example.com");
        var expectedResult = new ModifyResult(ModifyResultCode.Success, "");
        _mockInnerService.Setup(x => x.ModifyUserAsync(request)).ReturnsAsync(expectedResult);
        _mockInnerService
            .Setup(x => x.RotateCurrentUserKeyAsync(KeyType.SymmetricEncryption))
            .ReturnsAsync(expectedResult);

        Assert.Equal(expectedResult, await _decorator.ModifyUserAsync(request));
        Assert.Equal(
            expectedResult,
            await _decorator.RotateCurrentUserKeyAsync(KeyType.SymmetricEncryption)
        );
        _mockAuthorizationProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ModificationServiceAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ModificationServiceAuthorizationDecorator(_mockInnerService.Object, null!)
        );
}
