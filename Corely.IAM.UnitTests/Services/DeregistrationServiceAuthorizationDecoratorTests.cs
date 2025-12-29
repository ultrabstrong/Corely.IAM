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
    public async Task DeregisterGroupAsync_DelegatesToInner()
    {
        var request = new DeregisterGroupRequest(Guid.CreateVersion7());
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
        var request = new DeregisterRoleRequest(Guid.CreateVersion7());
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
        var request = new DeregisterPermissionRequest(Guid.CreateVersion7());
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
        var request = new DeregisterUsersFromGroupRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
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
        var request = new DeregisterRolesFromGroupRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
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
        var request = new DeregisterRolesFromUserRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
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
        var request = new DeregisterPermissionsFromRoleRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
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

    #region DeregisterUserAsync

    [Fact]
    public async Task DeregisterUserAsync_Succeeds_WhenHasUserContext()
    {
        var expectedResult = new DeregisterUserResult(
            DeregisterUserResultCode.Success,
            string.Empty
        );
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.DeregisterUserAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterUserAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterUserAsync(), Times.Once);
    }

    [Fact]
    public async Task DeregisterUserAsync_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(false);

        var result = await _decorator.DeregisterUserAsync();

        Assert.Equal(DeregisterUserResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.DeregisterUserAsync(), Times.Never);
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
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService.Setup(x => x.DeregisterAccountAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterAccountAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterAccountAsync(), Times.Once);
    }

    [Fact]
    public async Task DeregisterAccountAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.DeregisterAccountAsync();

        Assert.Equal(DeregisterAccountResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.DeregisterAccountAsync(), Times.Never);
    }

    #endregion

    #region DeregisterUserFromAccountAsync

    [Fact]
    public async Task DeregisterUserFromAccountAsync_Succeeds_WhenHasAccountContext()
    {
        var request = new DeregisterUserFromAccountRequest(Guid.CreateVersion7());
        var expectedResult = new DeregisterUserFromAccountResult(
            DeregisterUserFromAccountResultCode.Success,
            string.Empty
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
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
        var request = new DeregisterUserFromAccountRequest(Guid.CreateVersion7());
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

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
