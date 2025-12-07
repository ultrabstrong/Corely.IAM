using Corely.IAM.Auth.Constants;
using Corely.IAM.Auth.Exceptions;
using Corely.IAM.Auth.Providers;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;

namespace Corely.IAM.UnitTests.Roles.Processors;

public class RoleProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IRoleProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly RoleProcessorAuthorizationDecorator _decorator;

    public RoleProcessorAuthorizationDecoratorTests()
    {
        _decorator = new RoleProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    #region CreateRoleAsync Tests

    [Fact]
    public async Task CreateRoleAsync_CallsAuthorizationProvider()
    {
        var request = new CreateRoleRequest("TestRole", 1);
        var expectedResult = new CreateRoleResult(CreateRoleResultCode.Success, "", 1);
        _mockInnerProcessor.Setup(x => x.CreateRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.ROLE_RESOURCE_TYPE, AuthAction.Create, null),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.CreateRoleAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateRoleAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var request = new CreateRoleRequest("TestRole", 1);
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.ROLE_RESOURCE_TYPE, AuthAction.Create, null)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    AuthAction.Create.ToString()
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() => _decorator.CreateRoleAsync(request));

        _mockInnerProcessor.Verify(
            x => x.CreateRoleAsync(It.IsAny<CreateRoleRequest>()),
            Times.Never
        );
    }

    #endregion

    #region CreateDefaultSystemRolesAsync Tests

    [Fact]
    public async Task CreateDefaultSystemRolesAsync_BypassesAuthorization()
    {
        var accountId = 1;

        await _decorator.CreateDefaultSystemRolesAsync(accountId);

        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(It.IsAny<string>(), It.IsAny<AuthAction>(), It.IsAny<int?>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.CreateDefaultSystemRolesAsync(accountId), Times.Once);
    }

    #endregion

    #region GetRoleAsync Tests

    [Fact]
    public async Task GetRoleAsyncById_CallsAuthorizationProviderWithResourceId()
    {
        var roleId = 5;
        var expectedRole = new Role { Id = roleId, Name = "TestRole" };
        _mockInnerProcessor.Setup(x => x.GetRoleAsync(roleId)).ReturnsAsync(expectedRole);

        var result = await _decorator.GetRoleAsync(roleId);

        Assert.Equal(expectedRole, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.ROLE_RESOURCE_TYPE, AuthAction.Read, roleId),
            Times.Once
        );
    }

    [Fact]
    public async Task GetRoleAsyncById_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var roleId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.ROLE_RESOURCE_TYPE, AuthAction.Read, roleId)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    AuthAction.Read.ToString(),
                    roleId
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() => _decorator.GetRoleAsync(roleId));

        _mockInnerProcessor.Verify(x => x.GetRoleAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetRoleAsyncByName_CallsAuthorizationProvider()
    {
        var roleName = "TestRole";
        var accountId = 1;
        var expectedRole = new Role { Id = 1, Name = roleName };
        _mockInnerProcessor
            .Setup(x => x.GetRoleAsync(roleName, accountId))
            .ReturnsAsync(expectedRole);

        var result = await _decorator.GetRoleAsync(roleName, accountId);

        Assert.Equal(expectedRole, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.ROLE_RESOURCE_TYPE, AuthAction.Read, null),
            Times.Once
        );
    }

    #endregion

    #region AssignPermissionsToRoleAsync Tests

    [Fact]
    public async Task AssignPermissionsToRoleAsync_CallsAuthorizationProvider()
    {
        var request = new AssignPermissionsToRoleRequest([1, 2], 5);
        var expectedResult = new AssignPermissionsToRoleResult(
            AssignPermissionsToRoleResultCode.Success,
            "",
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.AssignPermissionsToRoleAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AssignPermissionsToRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.ROLE_RESOURCE_TYPE, AuthAction.Update, 5),
            Times.Once
        );
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var request = new AssignPermissionsToRoleRequest([1, 2], 5);
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.ROLE_RESOURCE_TYPE, AuthAction.Update, 5)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    AuthAction.Update.ToString(),
                    5
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.AssignPermissionsToRoleAsync(request)
        );

        _mockInnerProcessor.Verify(
            x => x.AssignPermissionsToRoleAsync(It.IsAny<AssignPermissionsToRoleRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_BypassesAuthorization_WhenFlagSet()
    {
        var request = new AssignPermissionsToRoleRequest([1, 2], 5, BypassAuthorization: true);
        var expectedResult = new AssignPermissionsToRoleResult(
            AssignPermissionsToRoleResultCode.Success,
            "",
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.AssignPermissionsToRoleAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AssignPermissionsToRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(It.IsAny<string>(), It.IsAny<AuthAction>(), It.IsAny<int?>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.AssignPermissionsToRoleAsync(request), Times.Once);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RoleProcessorAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RoleProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );

    #endregion
}
