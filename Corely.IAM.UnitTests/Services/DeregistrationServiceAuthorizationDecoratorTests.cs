using Corely.IAM.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;

namespace Corely.IAM.UnitTests.Services;

public class DeregistrationServiceAuthorizationDecoratorTests
{
    private readonly Mock<IDeregistrationService> _mockInnerService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly DeregistrationServiceAuthorizationDecorator _decorator;

    public DeregistrationServiceAuthorizationDecoratorTests()
    {
        _decorator = new DeregistrationServiceAuthorizationDecorator(
            _mockInnerService.Object,
            _mockAuthorizationProvider.Object
        );
    }

    #region Passthrough Methods (No Authorization)

    [Fact]
    public async Task DeregisterUserAsync_DelegatesToInner()
    {
        var request = new DeregisterUserRequest(1);
        var expectedResult = new DeregisterUserResult(
            DeregisterUserResultCode.Success,
            string.Empty
        );
        _mockInnerService.Setup(x => x.DeregisterUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeregisterGroupAsync_DelegatesToInner()
    {
        var request = new DeregisterGroupRequest(1);
        var expectedResult = new DeregisterGroupResult(
            DeregisterGroupResultCode.Success,
            string.Empty
        );
        _mockInnerService.Setup(x => x.DeregisterGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeregisterRoleAsync_DelegatesToInner()
    {
        var request = new DeregisterRoleRequest(1);
        var expectedResult = new DeregisterRoleResult(
            DeregisterRoleResultCode.Success,
            string.Empty
        );
        _mockInnerService.Setup(x => x.DeregisterRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterRoleAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeregisterPermissionAsync_DelegatesToInner()
    {
        var request = new DeregisterPermissionRequest(1);
        var expectedResult = new DeregisterPermissionResult(
            DeregisterPermissionResultCode.Success,
            string.Empty
        );
        _mockInnerService
            .Setup(x => x.DeregisterPermissionAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterPermissionAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterPermissionAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeregisterUsersFromGroupAsync_DelegatesToInner()
    {
        var request = new DeregisterUsersFromGroupRequest([1, 2], 1);
        var expectedResult = new DeregisterUsersFromGroupResult(
            DeregisterUsersFromGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.DeregisterUsersFromGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterUsersFromGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterUsersFromGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeregisterRolesFromGroupAsync_DelegatesToInner()
    {
        var request = new DeregisterRolesFromGroupRequest([1, 2], 1);
        var expectedResult = new DeregisterRolesFromGroupResult(
            DeregisterRolesFromGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.DeregisterRolesFromGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterRolesFromGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterRolesFromGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeregisterRolesFromUserAsync_DelegatesToInner()
    {
        var request = new DeregisterRolesFromUserRequest([1, 2], 1);
        var expectedResult = new DeregisterRolesFromUserResult(
            DeregisterRolesFromUserResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.DeregisterRolesFromUserAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterRolesFromUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterRolesFromUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeregisterPermissionsFromRoleAsync_DelegatesToInner()
    {
        var request = new DeregisterPermissionsFromRoleRequest([1, 2], 1);
        var expectedResult = new DeregisterPermissionsFromRoleResult(
            DeregisterPermissionsFromRoleResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.DeregisterPermissionsFromRoleAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterPermissionsFromRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterPermissionsFromRoleAsync(request), Times.Once);
    }

    #endregion

    #region DeregisterAccountAsync

    [Fact]
    public async Task DeregisterAccountAsync_Succeeds_WhenHasAccountContext()
    {
        var expectedResult = new DeregisterAccountResult(
            DeregisterAccountResultCode.Success,
            string.Empty
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(true);
        _mockInnerService.Setup(x => x.DeregisterAccountAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterAccountAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterAccountAsync(), Times.Once);
    }

    [Fact]
    public async Task DeregisterAccountAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(false);

        var result = await _decorator.DeregisterAccountAsync();

        Assert.Equal(DeregisterAccountResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.DeregisterAccountAsync(), Times.Never);
    }

    #endregion

    #region DeregisterUserFromAccountAsync

    [Fact]
    public async Task DeregisterUserFromAccountAsync_Succeeds_WhenHasAccountContext()
    {
        var request = new DeregisterUserFromAccountRequest(1);
        var expectedResult = new DeregisterUserFromAccountResult(
            DeregisterUserFromAccountResultCode.Success,
            string.Empty
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(true);
        _mockInnerService
            .Setup(x => x.DeregisterUserFromAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterUserFromAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterUserFromAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeregisterUserFromAccountAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new DeregisterUserFromAccountRequest(1);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(false);

        var result = await _decorator.DeregisterUserFromAccountAsync(request);

        Assert.Equal(DeregisterUserFromAccountResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.DeregisterUserFromAccountAsync(It.IsAny<DeregisterUserFromAccountRequest>()),
            Times.Never
        );
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DeregistrationServiceAuthorizationDecorator(
                null!,
                _mockAuthorizationProvider.Object
            )
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DeregistrationServiceAuthorizationDecorator(_mockInnerService.Object, null!)
        );

    #endregion
}
