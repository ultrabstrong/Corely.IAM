using Corely.IAM.Permissions.Constants;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

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

    [Fact]
    public async Task CreateRoleAsync_CallsAuthorizationProvider()
    {
        var request = new CreateRoleRequest("TestRole", 1);
        var expectedResult = new CreateRoleResult(CreateRoleResultCode.Success, "", 1);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Create, PermissionConstants.ROLE_RESOURCE_TYPE, null)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor.Setup(x => x.CreateRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Create,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    null
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.CreateRoleAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateRoleAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new CreateRoleRequest("TestRole", 1);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Create, PermissionConstants.ROLE_RESOURCE_TYPE, null)
            )
            .ReturnsAsync(false);

        var result = await _decorator.CreateRoleAsync(request);

        Assert.Equal(CreateRoleResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.CreateRoleAsync(It.IsAny<CreateRoleRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CreateDefaultSystemRolesAsync_BypassesAuthorization()
    {
        var accountId = 1;

        await _decorator.CreateDefaultSystemRolesAsync(accountId);

        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.CreateDefaultSystemRolesAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task GetRoleAsyncById_CallsAuthorizationProviderWithResourceId()
    {
        var roleId = 5;
        var expectedResult = new GetRoleResult(
            GetRoleResultCode.Success,
            string.Empty,
            new Role { Id = roleId, Name = "TestRole" }
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, roleId)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor.Setup(x => x.GetRoleAsync(roleId)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetRoleAsync(roleId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    roleId
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetRoleAsyncById_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var roleId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, roleId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.GetRoleAsync(roleId);

        Assert.Equal(GetRoleResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.Role);
        _mockInnerProcessor.Verify(x => x.GetRoleAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetRoleAsyncByName_CallsAuthorizationProvider()
    {
        var roleName = "TestRole";
        var accountId = 1;
        var expectedResult = new GetRoleResult(
            GetRoleResultCode.Success,
            string.Empty,
            new Role { Id = 1, Name = roleName }
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, null)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.GetRoleAsync(roleName, accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetRoleAsync(roleName, accountId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, null),
            Times.Once
        );
    }

    [Fact]
    public async Task GetRoleAsyncByName_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var roleName = "TestRole";
        var accountId = 1;
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, null)
            )
            .ReturnsAsync(false);

        var result = await _decorator.GetRoleAsync(roleName, accountId);

        Assert.Equal(GetRoleResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.Role);
        _mockInnerProcessor.Verify(
            x => x.GetRoleAsync(It.IsAny<string>(), It.IsAny<int>()),
            Times.Never
        );
    }

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
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.ROLE_RESOURCE_TYPE, 5)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.AssignPermissionsToRoleAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AssignPermissionsToRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.ROLE_RESOURCE_TYPE, 5),
            Times.Once
        );
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new AssignPermissionsToRoleRequest([1, 2], 5);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.ROLE_RESOURCE_TYPE, 5)
            )
            .ReturnsAsync(false);

        var result = await _decorator.AssignPermissionsToRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.UnauthorizedError, result.ResultCode);
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
            x => x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.AssignPermissionsToRoleAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteRoleAsync_CallsAuthorizationProvider()
    {
        var roleId = 5;
        var expectedResult = new DeleteRoleResult(DeleteRoleResultCode.Success, "");
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    roleId
                )
            )
            .ReturnsAsync(true);
        _mockInnerProcessor.Setup(x => x.DeleteRoleAsync(roleId)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteRoleAsync(roleId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    roleId
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.DeleteRoleAsync(roleId), Times.Once);
    }

    [Fact]
    public async Task DeleteRoleAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var roleId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    roleId
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.DeleteRoleAsync(roleId);

        Assert.Equal(DeleteRoleResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(x => x.DeleteRoleAsync(It.IsAny<int>()), Times.Never);
    }

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
}
