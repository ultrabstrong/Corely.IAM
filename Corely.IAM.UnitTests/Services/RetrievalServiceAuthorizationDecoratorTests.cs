using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Services;

public class RetrievalServiceAuthorizationDecoratorTests
{
    private readonly Mock<IRetrievalService> _mockInnerService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly RetrievalServiceAuthorizationDecorator _decorator;

    public RetrievalServiceAuthorizationDecoratorTests()
    {
        _decorator = new RetrievalServiceAuthorizationDecorator(
            _mockInnerService.Object,
            _mockAuthorizationProvider.Object
        );
    }

    #region ListPermissionsAsync

    [Fact]
    public async Task ListPermissionsAsync_Succeeds_WhenHasAccountContext()
    {
        var expectedResult = new RetrieveListResult<Permission>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Permission>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.ListPermissionsAsync(null, null, 0, 25))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListPermissionsAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListPermissionsAsync(null, null, 0, 25), Times.Once);
    }

    [Fact]
    public async Task ListPermissionsAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ListPermissionsAsync();

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ListPermissionsAsync(null, null, It.IsAny<int>(), It.IsAny<int>()),
            Times.Never
        );
    }

    #endregion

    #region GetPermissionAsync

    [Fact]
    public async Task GetPermissionAsync_Succeeds_WhenHasAccountContext()
    {
        var permissionId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<Permission>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.GetPermissionAsync(permissionId, false))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetPermissionAsync(permissionId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetPermissionAsync(permissionId, false), Times.Once);
    }

    [Fact]
    public async Task GetPermissionAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.GetPermissionAsync(Guid.CreateVersion7());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.GetPermissionAsync(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    #endregion

    #region ListGroupsAsync

    [Fact]
    public async Task ListGroupsAsync_Succeeds_WhenHasAccountContext()
    {
        var expectedResult = new RetrieveListResult<Group>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Group>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.ListGroupsAsync(null, null, 0, 25))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListGroupsAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListGroupsAsync(null, null, 0, 25), Times.Once);
    }

    [Fact]
    public async Task ListGroupsAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ListGroupsAsync();

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ListGroupsAsync(null, null, It.IsAny<int>(), It.IsAny<int>()),
            Times.Never
        );
    }

    #endregion

    #region GetGroupAsync

    [Fact]
    public async Task GetGroupAsync_Succeeds_WhenHasAccountContext()
    {
        var groupId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<Group>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService.Setup(x => x.GetGroupAsync(groupId, false)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetGroupAsync(groupId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetGroupAsync(groupId, false), Times.Once);
    }

    [Fact]
    public async Task GetGroupAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.GetGroupAsync(Guid.CreateVersion7());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.GetGroupAsync(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    #endregion

    #region ListRolesAsync

    [Fact]
    public async Task ListRolesAsync_Succeeds_WhenHasAccountContext()
    {
        var expectedResult = new RetrieveListResult<Role>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Role>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.ListRolesAsync(null, null, 0, 25))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListRolesAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListRolesAsync(null, null, 0, 25), Times.Once);
    }

    [Fact]
    public async Task ListRolesAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ListRolesAsync();

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ListRolesAsync(null, null, It.IsAny<int>(), It.IsAny<int>()),
            Times.Never
        );
    }

    #endregion

    #region GetRoleAsync

    [Fact]
    public async Task GetRoleAsync_Succeeds_WhenHasAccountContext()
    {
        var roleId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<Role>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService.Setup(x => x.GetRoleAsync(roleId, false)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetRoleAsync(roleId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetRoleAsync(roleId, false), Times.Once);
    }

    [Fact]
    public async Task GetRoleAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.GetRoleAsync(Guid.CreateVersion7());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.GetRoleAsync(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    #endregion

    #region ListUsersAsync

    [Fact]
    public async Task ListUsersAsync_Succeeds_WhenHasAccountContext()
    {
        var expectedResult = new RetrieveListResult<User>(
            RetrieveResultCode.Success,
            "",
            PagedResult<User>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.ListUsersAsync(null, null, 0, 25))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListUsersAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListUsersAsync(null, null, 0, 25), Times.Once);
    }

    [Fact]
    public async Task ListUsersAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ListUsersAsync();

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ListUsersAsync(null, null, It.IsAny<int>(), It.IsAny<int>()),
            Times.Never
        );
    }

    #endregion

    #region GetUserAsync

    [Fact]
    public async Task GetUserAsync_Succeeds_WhenHasAccountContext()
    {
        var userId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<User>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService.Setup(x => x.GetUserAsync(userId, false)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetUserAsync(userId, false), Times.Once);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.GetUserAsync(Guid.CreateVersion7());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.GetUserAsync(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    #endregion

    #region ListAccountsAsync

    [Fact]
    public async Task ListAccountsAsync_Succeeds_WhenHasUserContext()
    {
        var expectedResult = new RetrieveListResult<Account>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Account>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.ListAccountsAsync(null, null, 0, 25))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListAccountsAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListAccountsAsync(null, null, 0, 25), Times.Once);
    }

    [Fact]
    public async Task ListAccountsAsync_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(false);

        var result = await _decorator.ListAccountsAsync();

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ListAccountsAsync(null, null, It.IsAny<int>(), It.IsAny<int>()),
            Times.Never
        );
    }

    #endregion

    #region GetAccountAsync

    [Fact]
    public async Task GetAccountAsync_Succeeds_WhenHasAccountContext()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<Account>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.GetAccountAsync(accountId, false))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetAccountAsync(accountId, false), Times.Once);
    }

    [Fact]
    public async Task GetAccountAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.GetAccountAsync(Guid.CreateVersion7());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.GetAccountAsync(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RetrievalServiceAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RetrievalServiceAuthorizationDecorator(_mockInnerService.Object, null!)
        );

    #endregion
}
