using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Models;
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
    public async Task ListPermissions_Succeeds_WhenHasAccountContext()
    {
        var expectedResult = new RetrieveListResult<Permission>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Permission>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.ListPermissionsAsync(It.IsAny<ListPermissionsRequest>()))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListPermissionsAsync(new ListPermissionsRequest());

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(
            x => x.ListPermissionsAsync(It.IsAny<ListPermissionsRequest>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ListPermissions_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ListPermissionsAsync(new ListPermissionsRequest());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ListPermissionsAsync(It.IsAny<ListPermissionsRequest>()),
            Times.Never
        );
    }

    #endregion

    #region GetPermissionAsync

    [Fact]
    public async Task GetPermission_Succeeds_WhenHasAccountContext()
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
    public async Task GetPermission_ReturnsUnauthorized_WhenNoAccountContext()
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
    public async Task ListGroups_Succeeds_WhenHasAccountContext()
    {
        var expectedResult = new RetrieveListResult<Group>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Group>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.ListGroupsAsync(It.IsAny<ListGroupsRequest>()))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListGroupsAsync(new ListGroupsRequest());

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListGroupsAsync(It.IsAny<ListGroupsRequest>()), Times.Once);
    }

    [Fact]
    public async Task ListGroups_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ListGroupsAsync(new ListGroupsRequest());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ListGroupsAsync(It.IsAny<ListGroupsRequest>()),
            Times.Never
        );
    }

    #endregion

    #region GetGroupAsync

    [Fact]
    public async Task GetGroup_Succeeds_WhenHasAccountContext()
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
    public async Task GetGroup_ReturnsUnauthorized_WhenNoAccountContext()
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
    public async Task ListRoles_Succeeds_WhenHasAccountContext()
    {
        var expectedResult = new RetrieveListResult<Role>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Role>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.ListRolesAsync(It.IsAny<ListRolesRequest>()))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListRolesAsync(new ListRolesRequest());

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListRolesAsync(It.IsAny<ListRolesRequest>()), Times.Once);
    }

    [Fact]
    public async Task ListRoles_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ListRolesAsync(new ListRolesRequest());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.ListRolesAsync(It.IsAny<ListRolesRequest>()), Times.Never);
    }

    #endregion

    #region GetRoleAsync

    [Fact]
    public async Task GetRole_Succeeds_WhenHasAccountContext()
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
    public async Task GetRole_ReturnsUnauthorized_WhenNoAccountContext()
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
    public async Task ListUsers_Succeeds_WhenHasAccountContext()
    {
        var expectedResult = new RetrieveListResult<User>(
            RetrieveResultCode.Success,
            "",
            PagedResult<User>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>()))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListUsersAsync(new ListUsersRequest());

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>()), Times.Once);
    }

    [Fact]
    public async Task ListUsers_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ListUsersAsync(new ListUsersRequest());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>()), Times.Never);
    }

    #endregion

    #region GetUserAsync

    [Fact]
    public async Task GetUser_Succeeds_WhenHasUserContext()
    {
        var userId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<User>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.GetUserAsync(userId, false)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetUserAsync(userId, false), Times.Once);
    }

    [Fact]
    public async Task GetUser_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(false);

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
    public async Task ListAccounts_Succeeds_WhenHasUserContext()
    {
        var expectedResult = new RetrieveListResult<Account>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Account>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.ListAccountsAsync(It.IsAny<ListAccountsRequest>()))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListAccountsAsync(new ListAccountsRequest());

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(
            x => x.ListAccountsAsync(It.IsAny<ListAccountsRequest>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ListAccounts_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(false);

        var result = await _decorator.ListAccountsAsync(new ListAccountsRequest());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ListAccountsAsync(It.IsAny<ListAccountsRequest>()),
            Times.Never
        );
    }

    #endregion

    #region GetAccountAsync

    [Fact]
    public async Task GetAccount_Succeeds_WhenHasAccountContext()
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
    public async Task GetAccount_ReturnsUnauthorized_WhenNoAccountContext()
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

    #region GetAccountSymmetricEncryptionProviderAsync

    [Fact]
    public async Task GetAccountSymmetricEncryptionProvider_Succeeds_WhenHasAccountContext()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.GetAccountSymmetricEncryptionProviderAsync(accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountSymmetricEncryptionProviderAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(
            x => x.GetAccountSymmetricEncryptionProviderAsync(accountId),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAccountSymmetricEncryptionProvider_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.GetAccountSymmetricEncryptionProviderAsync(
            Guid.CreateVersion7()
        );

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.GetAccountSymmetricEncryptionProviderAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    #endregion

    #region GetAccountAsymmetricEncryptionProviderAsync

    [Fact]
    public async Task GetAccountAsymmetricEncryptionProvider_Succeeds_WhenHasAccountContext()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.GetAccountAsymmetricEncryptionProviderAsync(accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountAsymmetricEncryptionProviderAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(
            x => x.GetAccountAsymmetricEncryptionProviderAsync(accountId),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAccountAsymmetricEncryptionProvider_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.GetAccountAsymmetricEncryptionProviderAsync(
            Guid.CreateVersion7()
        );

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.GetAccountAsymmetricEncryptionProviderAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    #endregion

    #region GetAccountAsymmetricSignatureProviderAsync

    [Fact]
    public async Task GetAccountAsymmetricSignatureProvider_Succeeds_WhenHasAccountContext()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.GetAccountAsymmetricSignatureProviderAsync(accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountAsymmetricSignatureProviderAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(
            x => x.GetAccountAsymmetricSignatureProviderAsync(accountId),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAccountAsymmetricSignatureProvider_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.GetAccountAsymmetricSignatureProviderAsync(
            Guid.CreateVersion7()
        );

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.GetAccountAsymmetricSignatureProviderAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    #endregion

    #region GetUserSymmetricEncryptionProviderAsync

    [Fact]
    public async Task GetUserSymmetricEncryptionProvider_Succeeds_WhenHasUserContext()
    {
        var expectedResult = new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.GetUserSymmetricEncryptionProviderAsync())
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserSymmetricEncryptionProviderAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetUserSymmetricEncryptionProviderAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUserSymmetricEncryptionProvider_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(false);

        var result = await _decorator.GetUserSymmetricEncryptionProviderAsync();

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.GetUserSymmetricEncryptionProviderAsync(), Times.Never);
    }

    #endregion

    #region GetUserAsymmetricEncryptionProviderAsync

    [Fact]
    public async Task GetUserAsymmetricEncryptionProvider_Succeeds_WhenHasUserContext()
    {
        var expectedResult = new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.GetUserAsymmetricEncryptionProviderAsync())
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserAsymmetricEncryptionProviderAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetUserAsymmetricEncryptionProviderAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUserAsymmetricEncryptionProvider_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(false);

        var result = await _decorator.GetUserAsymmetricEncryptionProviderAsync();

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.GetUserAsymmetricEncryptionProviderAsync(), Times.Never);
    }

    #endregion

    #region GetUserAsymmetricSignatureProviderAsync

    [Fact]
    public async Task GetUserAsymmetricSignatureProvider_Succeeds_WhenHasUserContext()
    {
        var expectedResult = new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.GetUserAsymmetricSignatureProviderAsync())
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserAsymmetricSignatureProviderAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetUserAsymmetricSignatureProviderAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUserAsymmetricSignatureProvider_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(false);

        var result = await _decorator.GetUserAsymmetricSignatureProviderAsync();

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.GetUserAsymmetricSignatureProviderAsync(), Times.Never);
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
